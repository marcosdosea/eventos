using AutoMapper;
using EventoWeb.Models;
using Core;
using Mapster;

namespace EventoWeb.Mappers;

public class AreaInteresseMapper
{
    public static AreaInteresseModel ToViewModel(Areainteresse areaInteresse)
    {
        return areaInteresse.Adapt<AreaInteresseModel>();
    }
    public static Areainteresse ToEntity(AreaInteresseModel areaInteresseModel)
    {
        return areaInteresseModel.Adapt<Areainteresse>();
    }

    public static IEnumerable<AreaInteresseModel> ToViewModelList(IEnumerable<Areainteresse> areaInteresseList)
    {
        return areaInteresseList.Adapt<IEnumerable<AreaInteresseModel>>();
    }

    public static IEnumerable<Areainteresse> ToEntityList(IEnumerable<AreaInteresseModel> areaInteresseModelList)
    {
        return areaInteresseModelList.Adapt<IEnumerable<Areainteresse>>();
    }
}