using AutoMapper;
using Teleradiologia.Application.Dtos.Account;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Mapping;

public class GeneralProfile : Profile
{
    public GeneralProfile()
    {
        CreateMap<Usuario, UsuarioDto>();
    }
}
