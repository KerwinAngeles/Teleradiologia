using AutoMapper;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Application.Interfaces.Services;

namespace Teleradiologia.Application.Services;

public class GenericService<TSaveDto, TDto, TEntity>(
    IGenericRepository<TEntity> repository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IGenericService<TSaveDto, TDto, TEntity>
    where TSaveDto : class
    where TDto : class
    where TEntity : class
{
    protected IGenericRepository<TEntity> Repository { get; } = repository;
    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    protected IMapper Mapper { get; } = mapper;

    public virtual async Task<BaseResponse<TDto>> Add(TSaveDto dto, CancellationToken ct)
    {
        var entity = Mapper.Map<TEntity>(dto);

        Repository.Add(entity);
        await UnitOfWork.SaveChangesAsync(ct);

        return BaseResponse<TDto>.Success(Mapper.Map<TDto>(entity));
    }

    public virtual async Task<BaseResponse<TDto>> Update(TSaveDto dto, Guid id, CancellationToken ct)
    {
        var entity = await Repository.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return BaseResponse<TDto>.Fail($"No existe el registro {id}.", ErrorCode.NoEncontrado);
        }

        // Sobre la entidad rastreada, para no pisar las columnas que el DTO no trae.
        Mapper.Map(dto, entity);
        await UnitOfWork.SaveChangesAsync(ct);

        return BaseResponse<TDto>.Success(Mapper.Map<TDto>(entity));
    }

    public virtual async Task<BaseResponse<bool>> Delete(Guid id, CancellationToken ct)
    {
        var entity = await Repository.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return BaseResponse<bool>.Fail($"No existe el registro {id}.", ErrorCode.NoEncontrado);
        }

        Repository.Delete(entity);
        await UnitOfWork.SaveChangesAsync(ct);

        return BaseResponse<bool>.Success(true);
    }

    public virtual async Task<BaseResponse<List<TDto>>> GetAll(CancellationToken ct)
    {
        var entities = await Repository.GetAllAsync(ct);
        return BaseResponse<List<TDto>>.Success(Mapper.Map<List<TDto>>(entities));
    }

    public virtual async Task<BaseResponse<TDto>> GetById(Guid id, CancellationToken ct)
    {
        var entity = await Repository.GetByIdAsync(id, ct);

        return entity is null
            ? BaseResponse<TDto>.Fail($"No existe el registro {id}.", ErrorCode.NoEncontrado)
            : BaseResponse<TDto>.Success(Mapper.Map<TDto>(entity));
    }
}
