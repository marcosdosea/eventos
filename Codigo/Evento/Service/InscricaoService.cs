using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Service
{
	public class InscricaoService : IInscricaoService
	{
		private readonly EventoContext _context;
		private readonly UserManager<UsuarioIdentity> _userManager;

		public InscricaoService(EventoContext context, UserManager<UsuarioIdentity> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		public uint CreateInscricaoEvento(Inscricaopessoaevento inscricaopessoaevento)
		{
			if (PessoaJaInscrita(inscricaopessoaevento.IdPessoa, inscricaopessoaevento.IdEvento))
				throw new Exception("Esta pessoa já está inscrita neste evento.");

			inscricaopessoaevento.DataInscricao = DateTime.Now;
			_context.Add(inscricaopessoaevento);
			_context.SaveChanges();

			var evento = _context.Eventos
				.Include(e => e.Inscricaopessoaeventos)
				.FirstOrDefault(e => e.Id == inscricaopessoaevento.IdEvento);

			if (evento != null)
			{
				var quantidadeParticipantes = evento.Inscricaopessoaeventos
					.Count(i => i.IdPapel == 4);

				evento.VagasDisponiveis = evento.VagasOfertadas - quantidadeParticipantes;
				_context.SaveChanges();
			}

			return inscricaopessoaevento.Id;
		}

		public void CreateInscricaoSubEvento(Inscricaopessoasubevento inscricaopessoasubevento)
		{
			_context.Add(inscricaopessoasubevento);
			_context.SaveChanges();
		}

		public async Task AtribuirPapelNoEventoAsync(Pessoa pessoa, uint idEvento, int idPapel)
		{
			var pessoaExistente = _context.Pessoas.FirstOrDefault(p => p.Cpf == pessoa.Cpf);
			if (pessoaExistente == null)
			{
				_context.Pessoas.Add(pessoa);
				await _context.SaveChangesAsync();
				pessoaExistente = pessoa;
			}

			bool jaVinculado = _context.Inscricaopessoaeventos
				.Any(i => i.IdPessoa == pessoaExistente.Id
					   && i.IdEvento == idEvento
					   && i.IdPapel == idPapel);

			if (jaVinculado)
				throw new Exception("Esta pessoa já está vinculada a este evento com este papel.");

			var inscricao = new Inscricaopessoaevento
			{
				IdPessoa = pessoaExistente.Id,
				IdEvento = idEvento,
				IdPapel = idPapel,
				DataInscricao = DateTime.Now,
				Status = "A",
				NomeCracha = !string.IsNullOrWhiteSpace(pessoaExistente.NomeCracha)
					? pessoaExistente.NomeCracha
					: pessoaExistente.Nome,
				FrequenciaFinal = 0
			};

			_context.Inscricaopessoaeventos.Add(inscricao);
			await _context.SaveChangesAsync();
		}

		public async Task DeletePessoaPapelAsync(uint idPessoa, uint idEvento, uint idPapel, string cpf)
		{
			var pessoa = await _context.Pessoas
				.FirstOrDefaultAsync(p => p.Id == idPessoa && p.Cpf == cpf);

			if (pessoa == null)
				throw new Exception("Pessoa não encontrada com o CPF informado.");

			var inscricao = await _context.Inscricaopessoaeventos
				.FirstOrDefaultAsync(i => i.IdPessoa == idPessoa
									   && i.IdEvento == idEvento
									   && i.IdPapel == (int)idPapel);

			if (inscricao != null)
			{
				_context.Inscricaopessoaeventos.Remove(inscricao);
				await _context.SaveChangesAsync();
			}

			if (idPapel != 4)
			{
				bool aindaPossuiPapel = await _context.Inscricaopessoaeventos
					.AnyAsync(i => i.IdPessoa == idPessoa && i.IdPapel == (int)idPapel);

				if (!aindaPossuiPapel)
					await RemoverRoleIdentityAsync(cpf, idPapel);
			}
		}

		public IEnumerable<Inscricaopessoaevento> GetByEvento(uint idEvento)
		{
			return _context.Inscricaopessoaeventos
				.Include(i => i.IdPessoaNavigation)
				.Where(i => i.IdEvento == idEvento)
				.AsNoTracking()
				.ToList();
		}

		public IEnumerable<Inscricaopessoaevento> GetByEventoAndPapel(uint idEvento, int idPapel)
		{
			return _context.Inscricaopessoaeventos
				.Include(i => i.IdPessoaNavigation)
				.Where(i => i.IdEvento == idEvento && i.IdPapel == idPapel)
				.AsNoTracking()
				.ToList();
		}

		public IEnumerable<Inscricaopessoasubevento> GetSubByEvento(uint idEvento)
		{
			return _context.Inscricaopessoasubeventos
				.Include(i => i.IdPessoaNavigation)
				.Include(i => i.IdSubEventoNavigation)
				.Where(i => i.IdSubEventoNavigation.IdEvento == idEvento)
				.AsNoTracking()
				.ToList();
		}

		public IEnumerable<Inscricaopessoaevento> GetAllEventsByUserId(string username)
		{
			return _context.Inscricaopessoaeventos
				.Include(i => i.IdEventoNavigation)
				.Where(i => i.IdPessoaNavigation.Cpf.Contains(username))
				.ToList();
		}

		public int GetPapelPessoaByEvento(uint idPessoa, uint idEvento)
		{
			return _context.Inscricaopessoaeventos
				.Where(i => i.IdEvento == idEvento && i.IdPessoa == idPessoa)
				.Select(i => i.IdPapel)
				.FirstOrDefault();
		}

		public Inscricaopessoaevento GetGestorInEvent(string username, uint idEvento)
		{
			return _context.Inscricaopessoaeventos
				.Include(i => i.IdPessoaNavigation)
				.FirstOrDefault(i => i.IdPessoaNavigation.Cpf.Contains(username)
								  && i.IdPapel == 2
								  && i.IdEvento == idEvento);
		}

		public Inscricaopessoaevento GetColaboradorInEvent(string username, uint idEvento)
		{
			return _context.Inscricaopessoaeventos
				.Include(i => i.IdPessoaNavigation)
				.FirstOrDefault(i => i.IdPessoaNavigation.Cpf.Contains(username)
								  && i.IdPapel == 3
								  && i.IdEvento == idEvento);
		}

		public Inscricaopessoaevento GetById(uint id)
			=> _context.Inscricaopessoaeventos.Find(id);

		public IEnumerable<Inscricaopessoaevento> GetAll()
			=> _context.Inscricaopessoaeventos.ToList();

		public void Update(Inscricaopessoaevento inscricao)
		{
			_context.Inscricaopessoaeventos.Update(inscricao);
			_context.SaveChanges();
		}

		public void Delete(uint id)
		{
			var inscricao = GetById(id);
			if (inscricao != null)
			{
				_context.Inscricaopessoaeventos.Remove(inscricao);
				_context.SaveChanges();
			}
		}

		public bool PessoaJaInscrita(uint idPessoa, uint idEvento)
		{
			return _context.Inscricaopessoaeventos
				.Any(i => i.IdPessoa == idPessoa && i.IdEvento == idEvento);
		}

		private async Task RemoverRoleIdentityAsync(string cpf, uint idPapel)
		{
			string cpfLimpo = cpf.Replace(".", "").Replace("-", "");
			var user = await _userManager.FindByNameAsync(cpfLimpo);
			if (user == null) return;

			string role = idPapel switch
			{
				1 => "ADMINISTRADOR",
				2 => "GESTOR",
				3 => "COLABORADOR",
				_ => throw new ArgumentException("Papel inválido para remoção de role.")
			};

			if (await _userManager.IsInRoleAsync(user, role))
				await _userManager.RemoveFromRoleAsync(user, role);
		}
	}
}