document.addEventListener('DOMContentLoaded', function() {
    let slideIndex = 0;
    const slides = document.querySelectorAll('.carrossel-slide');
    const indicators = document.querySelectorAll('.carrossel-indicators .indicator');

    function showSlide(n) {
        slides.forEach((slide, index) => {
            slide.style.display = (index === n) ? 'block' : 'none';
        });

        indicators.forEach((indicator, index) => {
            indicator.classList.toggle('active', index === n);
        });
    }

    function moveSlide(n) {
        slideIndex = (slideIndex + n + slides.length) % slides.length;
        showSlide(slideIndex);
    }

    function currentSlide(n) {
        slideIndex = n;
        showSlide(slideIndex);
    }

    document.querySelector('.prev').addEventListener('click', () => moveSlide(-1));
    document.querySelector('.next').addEventListener('click', () => moveSlide(1));
    indicators.forEach((indicator, index) => {
        indicator.addEventListener('click', () => currentSlide(index));
    });

    showSlide(slideIndex);
});
