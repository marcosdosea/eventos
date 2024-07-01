using Microsoft.AspNetCore.Http;

namespace Core.Service
{
    public interface IModelocrachaService
    {
        uint Create(Modelocracha modelocracha);
        void Edit(Modelocracha modelocracha);
        void Delete(uint id);
        Modelocracha Get(uint id);
        IEnumerable<Modelocracha> GetAll();
        bool IsImage(IFormFile modelocrachaModelLogotipoFile);
    }
}
