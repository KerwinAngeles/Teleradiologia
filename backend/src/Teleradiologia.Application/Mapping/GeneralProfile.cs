using AutoMapper;
using Teleradiologia.Application.Dtos.Account;
using Teleradiologia.Application.Dtos.Eventos;
using Teleradiologia.Application.Dtos.Hospitales;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Mapping;

public class GeneralProfile : Profile
{
    public GeneralProfile()
    {
        CreateMap<Usuario, UsuarioDto>();
        CreateMap<Evento, EventoDto>();
        CreateMap<Hospital, HospitalDto>();
        CreateMap<EstablecimientoCatalogo, EstablecimientoCatalogoDto>();
    }
}
