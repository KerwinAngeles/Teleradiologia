using Teleradiologia.Application.Common;

namespace Teleradiologia.Application.Interfaces.Services;

public interface IGenericService<TSaveDto, TDto, TEntity>
    where TSaveDto : class
    where TDto : class
    where TEntity : class
{
    Task<BaseResponse<TDto>> Add(TSaveDto dto, CancellationToken ct);

    Task<BaseResponse<TDto>> Update(TSaveDto dto, Guid id, CancellationToken ct);

    Task<BaseResponse<bool>> Delete(Guid id, CancellationToken ct);

    Task<BaseResponse<List<TDto>>> GetAll(CancellationToken ct);

    Task<BaseResponse<TDto>> GetById(Guid id, CancellationToken ct);
}
