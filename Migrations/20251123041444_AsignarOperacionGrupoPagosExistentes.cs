using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class AsignarOperacionGrupoPagosExistentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
    CREATE TEMP TABLE tmp_pagos_grupos AS
    WITH pagos_ordenados AS (
        SELECT 
            id_pago, 
            alumno_id, 
            fecha, 
            strftime('%s', fecha) AS seg_actual,
            LAG(strftime('%s', fecha)) OVER (PARTITION BY alumno_id ORDER BY fecha, id_pago) AS seg_prev
        FROM pagos
    ),
    marcados AS (
        SELECT 
            id_pago, 
            alumno_id, 
            fecha,
            CASE 
                WHEN seg_prev IS NULL THEN 1 
                WHEN seg_actual - seg_prev > 2 THEN 1 
                ELSE 0 
            END AS es_nuevo
        FROM pagos_ordenadosame
    ),
    acumulado AS (
        SELECT 
            id_pago, 
            alumno_id, 
            SUM(es_nuevo) OVER (PARTITION BY alumno_id ORDER BY fecha, id_pago ROWS UNBOUNDED PRECEDING) AS cluster_num
        FROM marcados
    )
    SELECT id_pago, alumno_id, cluster_num FROM acumulado;

    CREATE TEMP TABLE tmp_cluster_guid AS
    SELECT alumno_id, cluster_num, lower(hex(randomblob(16))) AS cluster_guid
    FROM tmp_pagos_grupos
    GROUP BY alumno_id, cluster_num;

    UPDATE pagos
    SET operacion_grupo_id = (
        SELECT cluster_guid 
        FROM tmp_cluster_guid g
        JOIN tmp_pagos_grupos c 
            ON c.alumno_id = g.alumno_id AND c.cluster_num = g.cluster_num
        WHERE c.id_pago = pagos.id_pago
    )
    WHERE IFNULL(operacion_grupo_id, '') = '';

    DROP TABLE tmp_cluster_guid;
    DROP TABLE tmp_pagos_grupos;
    ");
}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
