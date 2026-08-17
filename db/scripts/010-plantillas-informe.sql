-- Plantillas de informe por radiólogo. Cada una guarda sus secciones como jsonb ordenado:
-- [{ "titulo": "Técnica", "contenido": "...", "orden": 0 }, ...]
--
-- Las secciones van en jsonb y no en tabla aparte porque siempre se leen y se escriben
-- completas: no hay consulta que pida una sección suelta.
--
--   psql -U teleradiologia -d teleradiologia -f db/scripts/010-plantillas-informe.sql

BEGIN;

CREATE TABLE IF NOT EXISTS "PlantillasInforme" (
    "Id"              uuid                     NOT NULL,
    "RadiologoId"     uuid                     NOT NULL,
    "Nombre"          character varying(200)   NOT NULL,
    "Modalidad"       character varying(16),
    "RegionAnatomica" character varying(120),
    "Descripcion"     character varying(500),
    "Secciones"       jsonb                    NOT NULL DEFAULT '[]'::jsonb,
    "Favorita"        boolean                  NOT NULL DEFAULT false,
    "VecesUsada"      integer                  NOT NULL DEFAULT 0,
    "Activa"          boolean                  NOT NULL DEFAULT true,
    "CreatedAt"       timestamp with time zone NOT NULL,
    "CreatedBy"       character varying(256),
    "LastModified"    timestamp with time zone,
    "LastModifiedBy"  character varying(256),
    CONSTRAINT "PK_PlantillasInforme" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PlantillasInforme_Usuarios_RadiologoId" FOREIGN KEY ("RadiologoId")
        REFERENCES "Usuarios" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_PlantillasInforme_RadiologoId" ON "PlantillasInforme" ("RadiologoId");
CREATE INDEX IF NOT EXISTS "IX_PlantillasInforme_Modalidad" ON "PlantillasInforme" ("Modalidad");
-- Un radiólogo no puede tener dos plantillas activas con el mismo nombre.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PlantillasInforme_RadiologoId_Nombre"
    ON "PlantillasInforme" ("RadiologoId", lower("Nombre")) WHERE "Activa";

COMMIT;
