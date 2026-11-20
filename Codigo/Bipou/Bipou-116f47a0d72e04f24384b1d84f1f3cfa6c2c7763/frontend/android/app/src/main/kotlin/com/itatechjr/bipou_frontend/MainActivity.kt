package com.itatechjr.bipou_frontend

import android.content.Context
import android.media.AudioAttributes
import android.media.AudioFormat
import android.media.AudioManager
import android.media.AudioTrack
import android.media.ToneGenerator
import android.os.Build
import android.os.VibrationEffect
import android.os.Vibrator
import android.os.VibratorManager
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel
import java.util.concurrent.Executors
import kotlin.math.PI
import kotlin.math.min
import kotlin.math.roundToInt
import kotlin.math.sin

class MainActivity : FlutterActivity() {
    companion object {
        private const val CHANNEL = "com.itatechjr.bipou_frontend/scanner_feedback"
        private const val SAMPLE_RATE = 44100
    }

    private val audioExecutor = Executors.newSingleThreadExecutor()

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, CHANNEL)
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "playSuccess" -> {
                        triggerSingleVibration(80L)
                        playToneAsync(
                            listOf(ToneSegment(frequencyHz = 1480.0, durationMs = 110)),
                            ToneGenerator.TONE_PROP_BEEP,
                            110,
                        )
                        result.success(null)
                    }
                    "playAlert" -> {
                        triggerDoubleVibration()
                        playToneAsync(
                            listOf(
                                ToneSegment(frequencyHz = 280.0, durationMs = 140),
                                ToneSegment(frequencyHz = 0.0, durationMs = 70),
                                ToneSegment(frequencyHz = 280.0, durationMs = 160),
                            ),
                            ToneGenerator.TONE_SUP_ERROR,
                            240,
                        )
                        result.success(null)
                    }
                    "playDuplicate" -> {
                        triggerSingleVibration(45L)
                        playToneAsync(
                            listOf(ToneSegment(frequencyHz = 620.0, durationMs = 80)),
                            ToneGenerator.TONE_PROP_ACK,
                            80,
                        )
                        result.success(null)
                    }
                    else -> result.notImplemented()
                }
            }
    }

    override fun onDestroy() {
        audioExecutor.shutdownNow()
        super.onDestroy()
    }

    private fun triggerSingleVibration(durationMs: Long) {
        try {
            val vibrator = obtainVibrator() ?: return
            if (!vibrator.hasVibrator()) return
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                vibrator.vibrate(
                    VibrationEffect.createOneShot(
                        durationMs,
                        VibrationEffect.DEFAULT_AMPLITUDE,
                    ),
                )
            } else {
                @Suppress("DEPRECATION")
                vibrator.vibrate(durationMs)
            }
        } catch (_: Throwable) {
            // Dispositivo ou emulador sem motor haptico nao deve falhar.
        }
    }

    private fun triggerDoubleVibration() {
        try {
            val vibrator = obtainVibrator() ?: return
            if (!vibrator.hasVibrator()) return
            val pattern = longArrayOf(0L, 110L, 90L, 140L)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                vibrator.vibrate(VibrationEffect.createWaveform(pattern, -1))
            } else {
                @Suppress("DEPRECATION")
                vibrator.vibrate(pattern, -1)
            }
        } catch (_: Throwable) {
            // Dispositivo ou emulador sem motor haptico nao deve falhar.
        }
    }

    private fun obtainVibrator(): Vibrator? {
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            val manager = getSystemService(Context.VIBRATOR_MANAGER_SERVICE) as? VibratorManager
            manager?.defaultVibrator
        } else {
            @Suppress("DEPRECATION")
            getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator
        }
    }

    private fun playToneAsync(
        segments: List<ToneSegment>,
        fallbackTone: Int,
        fallbackDurationMs: Int,
    ) {
        audioExecutor.execute {
            if (!playPcmSegments(segments)) {
                playToneGeneratorFallback(fallbackTone, fallbackDurationMs)
            }
        }
    }

    private fun playPcmSegments(segments: List<ToneSegment>): Boolean {
        return try {
            val totalSamples = segments.sumOf { (SAMPLE_RATE * it.durationMs) / 1000 }
            if (totalSamples <= 0) return false

            val pcm = ShortArray(totalSamples)
            var offset = 0
            for (segment in segments) {
                val count = (SAMPLE_RATE * segment.durationMs) / 1000
                if (segment.frequencyHz > 0.0 && count > 0) {
                    val fadeSamples = min(count / 8, (SAMPLE_RATE * 10) / 1000)
                    for (i in 0 until count) {
                        val env = when {
                            fadeSamples <= 0 -> 1.0
                            i < fadeSamples -> i.toDouble() / fadeSamples
                            i >= count - fadeSamples -> (count - i).toDouble() / fadeSamples
                            else -> 1.0
                        }
                        val sample = sin(2.0 * PI * segment.frequencyHz * i / SAMPLE_RATE)
                        pcm[offset + i] = (sample * env * 0.45 * Short.MAX_VALUE).roundToInt().toShort()
                    }
                }
                offset += count
            }

            val track = AudioTrack.Builder()
                .setAudioAttributes(
                    AudioAttributes.Builder()
                        .setUsage(AudioAttributes.USAGE_NOTIFICATION_EVENT)
                        .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
                        .build(),
                )
                .setAudioFormat(
                    AudioFormat.Builder()
                        .setEncoding(AudioFormat.ENCODING_PCM_16BIT)
                        .setSampleRate(SAMPLE_RATE)
                        .setChannelMask(AudioFormat.CHANNEL_OUT_MONO)
                        .build(),
                )
                .setTransferMode(AudioTrack.MODE_STATIC)
                .setBufferSizeInBytes(pcm.size * 2)
                .build()

            track.write(pcm, 0, pcm.size)
            track.play()
            val totalDurationMs = segments.sumOf { it.durationMs }.toLong()
            Thread.sleep(totalDurationMs + 40L)
            track.stop()
            track.release()
            true
        } catch (_: Throwable) {
            false
        }
    }

    private fun playToneGeneratorFallback(toneType: Int, durationMs: Int) {
        var toneGenerator: ToneGenerator? = null
        try {
            toneGenerator = ToneGenerator(AudioManager.STREAM_NOTIFICATION, 80)
            toneGenerator.startTone(toneType, durationMs)
            Thread.sleep(durationMs.toLong() + 30L)
        } catch (_: Throwable) {
            // Ignora falha caso audio esteja indisponivel.
        } finally {
            try {
                toneGenerator?.release()
            } catch (_: Throwable) {
                // Ignora erro ao liberar recurso de audio.
            }
        }
    }

    private data class ToneSegment(
        val frequencyHz: Double,
        val durationMs: Int,
    )
}
