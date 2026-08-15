namespace Teleradiologia.Application.Estudios;

public record SubirEstudioRequest(IReadOnlyList<byte[]> ArchivosDicom, string HospitalOrigen, Guid SubidoPorId);
