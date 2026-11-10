using Core;

namespace EventoWeb.Models;

public class PreferenciasInteresseModel
{
    public uint AreaId { get; set; }
    public string Abrangencia { get; set; } = "brasil";
    public List<string> Estados { get; set; } = [];
    public IReadOnlyList<Pessoaareainteresse> Preferencias { get; set; } = [];
    public IEnumerable<Areainteresse> Areas { get; set; } = [];
    public IEnumerable<Estadosbrasil> EstadosDisponiveis { get; set; } = [];
    public bool Editando { get; set; }
}
