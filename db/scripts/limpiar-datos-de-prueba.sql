-- Vacía los datos clínicos de prueba dejando la configuración en pie.
--
-- Se borra: estudios, informes, pacientes, notificaciones y las dos bitácoras.
-- Se conserva: usuarios, hospitales, habilitaciones, plantillas y el catálogo del MISPAS.
--
-- Las imágenes siguen en Orthanc: esto solo limpia la base. Volver a subir los mismos
-- archivos crea registros nuevos sin problema.
--
--   psql -U teleradiologia -d teleradiologia -f db/scripts/limpiar-datos-de-prueba.sql

BEGIN;

-- Orden por las claves foráneas: lo que apunta va antes que lo apuntado.
DELETE FROM "Notificaciones";
DELETE FROM "AuditLogs";
DELETE FROM "Informes";
DELETE FROM "Estudios";
DELETE FROM "Pacientes";

-- La bitácora de cambios queda vacía también: todas sus filas describen datos que ya no existen.
DELETE FROM "Eventos";

COMMIT;
