namespace Core.Service;

public interface IAreaInteresseService

{
    uint Create(Areainteresse areainteresse);
    void Edit(Areainteresse areainteresse);
    void Delete(uint id);
    Areainteresse Get(uint id);
    IEnumerable<Areainteresse> GetAll();
    
    IEnumerable<Areainteresse> GetByNome(string nome);
}