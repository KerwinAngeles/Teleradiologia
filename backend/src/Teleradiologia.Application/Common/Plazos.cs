using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Common;

public static class Plazos
{
    public static int MinutosDe(Hospital? hospital, PrioridadEstudio prioridad, SlaOptions opciones) => prioridad switch
    {
        PrioridadEstudio.Stat => hospital?.SlaStatMinutos ?? opciones.StatMinutos,
        PrioridadEstudio.Urgente => hospital?.SlaUrgenteMinutos ?? opciones.UrgenteMinutos,
        _ => hospital?.SlaRutinaMinutos ?? opciones.RutinaMinutos,
    };

    public static DateTimeOffset CalcularLimite(
        DateTimeOffset recibido,
        Hospital? hospital,
        PrioridadEstudio prioridad,
        SlaOptions opciones) =>
        recibido.AddMinutes(MinutosDe(hospital, prioridad, opciones));

    public static EstadoSla Evaluar(Estudio estudio, SlaOptions opciones, DateTimeOffset ahora)
    {
        // Cerrado: el plazo ya no corre, solo interesa si se cumplió.
        if (estudio.InformadoAt is { } informado)
        {
            return informado <= estudio.FechaLimite ? EstadoSla.Cumplido : EstadoSla.Incumplido;
        }

        if (ahora >= estudio.FechaLimite)
        {
            return EstadoSla.Vencido;
        }

        var totalMinutos = (estudio.FechaLimite - estudio.CreatedAt).TotalMinutes;
        var restantes = (estudio.FechaLimite - ahora).TotalMinutes;

        return totalMinutos > 0 && restantes / totalMinutos * 100 <= opciones.UmbralPorVencerPorcentaje
            ? EstadoSla.PorVencer
            : EstadoSla.EnPlazo;
    }

    // Negativo cuando ya venció, para que el frontend muestre cuánto se pasó.
    public static int MinutosRestantes(Estudio estudio, DateTimeOffset ahora) =>
        (int)Math.Round(((estudio.InformadoAt ?? ahora) - estudio.FechaLimite).TotalMinutes * -1);
}
