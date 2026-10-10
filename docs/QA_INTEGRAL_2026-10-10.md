# CampusFix — implementación integral local (10-10-2026)

## Diagnóstico antes de modificar

Base efectiva confirmada mediante UserSecrets de Development, conexión PostgreSQL y `inet_server_addr`: localhost:5432/campusfix (::1). Respaldo previo: `C:\Users\lio13\AppData\Local\CampusFix\integral-2026-10-10\antes-20261010-112918.dump`. Se conserva además un respaldo por cada ejecución del aprovisionador.

8 áreas activas (no 15): Admisiones, Biblioteca, Bienestar estudiantil, Dirección Académica, Mantenimiento, Registro Académico, Secretaría Académica y Tecnologías de la Información. Inicialmente: 10 usuarios, 59 recursos, 27 tickets, 273 avisos; todas las áreas activas tenían managers vacío. Existían equipo de Bienestar, administrador, Fabián Puma, dos servicios y dos categorías: reutilizados por ID. Se conservan duplicados históricos desactivados y sedes repetidas; no se borraron ni se reactivaron automáticamente. Elementos padre preservados, incluido el de Bienestar, que actualmente depende de Dirección Académica.

QA-30: la condición de clasificación también dependía de tener un administrador activo. Faltaba esa relación; clasificar aparentaba éxito pero la condición seguía incumplida. Ahora informa la causa, valida responsables, guarda el resultado y permite clasificación a administradores del ámbito. Las lecturas ya no derivan silenciosamente snapshots históricos desde un servicio modificado; el backfill se aplica mediante migración explícita.

QA-31: el rol Solicitante guardado tenía panel=false/tablero=true, y el login enviaba siempre a panel; el guard terminaba en cuenta. Ahora el destino es tablero conforme a permisos, salvo flags explícitos mustChange/needsProfile. La pertenencia permanece guardada y el cambio voluntario de contraseña sigue disponible.

## Implementación y pruebas por etapa

- Configuración: área responsable y sharedAreaIds en recursos, categorías derivan el ámbito desde su servicio. Globales se conservan y solo el propietario los edita. Compartidos requieren administrar todas sus áreas para editar. Administrador no puede apropiarse de recursos cambiando el payload ni otorgarse áreas desde Elemento padre/managers.
- Equipos y asignaciones: elegibilidad por operationalAreas o pertenencia a equipo de área explícita; multiárea admitida. Catálogo administrativo filtrado en servidor e interfaz. Etiqueta de solicitudes: Responsable asignado.
- Formulario: Área solicitante preservada; Área de atención filtra servicio/categoría y limpia valores incompatibles. Ayuda general continúa disponible. Backend valida coherencia cuando el consumidor envía attentionAreaId; consumidores anteriores siguen admitidos.
- Configuración por área: selector persistente por usuario, Todas las áreas para propietario, global/compartido identificado, padres preservados, creación preselecciona ámbito.
- Verificación de etapa: backend y Angular compilan; 98 comprobaciones de negocio PostgreSQL más prueba de migración, 34 pruebas de Workspace/Auth de los cambios agrupados.

## Carga local ya ejecutada

Aprovisionador integrado: `--provision-local --local-approved`, requiere Development, host loopback y dirección real del servidor loopback. Sin `--apply` genera inventario y previsualiza sin escribir en BD. Con `--apply` realiza pg_dump, aplica migraciones, bloquea el lote y ejecuta transacción. Marcadores internos estables identifican recursos/tickets y origen sintético. Se usan validaciones de configuración, creación de tickets y eventos del dominio. No se manda correo externo; no se alteran claves existentes. Las credenciales nuevas se guardan fuera del repositorio en un directorio privado.

Migraciones aplicadas localmente: `20261010010000_ServiceRouting` y `20261010120000_ResourceScope`. La segunda obtiene ámbito de equipos legados solo desde referencias explícitas de servicios/categorías; soporta equipos compartidos y no inventa relaciones por nombre.

Resultado: 52 cuentas nuevas, 57 recursos nuevos, 40 tickets nuevos y 80 avisos de creación (40 para solicitantes, 40 para administradores). Se conservan los 27 tickets anteriores. Cada área tiene 5 solicitudes Reportado, 5 solicitantes distintos y 5 avisos iniciales no leídos. La consulta `database/verificar-aprovisionamiento-local.sql` verificó las ocho filas y cero eventos duplicados.

Segunda ejecución: usuarios 62→62, recursos 116→116, tickets 67→67 y avisos 353→353. Las dos ejecuciones completas y los inventarios antes/después están en el directorio privado. API/Angular reiniciados en localhost:5137 y localhost:4200 con la versión actual.

Pendiente de esta etapa de trabajo: regresión UI sobre la instalación cargada, pruebas finales y documento de accesos organizado. Las cuentas existentes conservan sus contraseñas; la comprobación de sesión de esas cuentas requiere acceso normal del propietario. Se retiró una opción de sesiones automáticas rechazada por revisión de aprobación; no existe endpoint ni comando para eludir autenticación.
