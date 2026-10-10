La fase posterior de mejoras está documentada en [QA_MEJORAS.md](QA_MEJORAS.md), con pruebas y comandos vigentes. Este documento conserva la evidencia histórica anterior.

# Verificación local y límites de producción

## Resultado de integración del 9 de octubre de 2026

Las consultas de solo lectura a `/login` en Vercel y `/api/health` en Vercel y Render devolvieron 200. Ambas rutas de salud declararon conexión PostgreSQL. Esto comprueba enrutamiento y conectividad en ese momento, no el funcionamiento de tickets de la versión publicada.

`frontend/campusfix-web/vercel.json` conserva primero la redirección `/api/:path*` a Render y después el fallback de Angular. El Dockerfile publica ASP.NET 10 en el puerto 10000. Desarrollo usa proxy `/api/**` a localhost:5137. No fue necesario cambiar estas configuraciones ni secretos.

El error de creación se reprodujo en PostgreSQL aislado: `Store.Next` emitía `RETURNING "Value` sin cerrar el identificador. La corrección es código local; no requiere migraciones ni cambios en datos. Se añadieron referencias de error 500 correlacionadas con los logs del backend.

## Repetir las pruebas sin producción

Desde la raíz del repositorio:

```powershell
dotnet run --project tests/CampusFix.Checks --no-restore
```

Para PostgreSQL, crear una base **vacía y desechable** en un servidor local independiente. El ejecutable rechaza hosts externos y bases que no empiecen por `campusfix_qa` antes de realizar migraciones. Nunca utilizar conexiones de Supabase ni importar datos reales.

```powershell
$env:CAMPUSFIX_TEST_POSTGRES='Host=127.0.0.1;Port=55439;Database=campusfix_qa;Username=campusfix_qa'
dotnet run --project tests/CampusFix.Checks --no-restore -- --postgres
```

El modo PostgreSQL migra el esquema de prueba y revierte los datos de su transacción. El modo HTTP necesita otra base vacía por ejecución:

```powershell
$env:CAMPUSFIX_TEST_POSTGRES='Host=127.0.0.1;Port=55439;Database=campusfix_qa_http;Username=campusfix_qa'
dotnet run --project tests/CampusFix.Checks --no-restore -- --http
```

HTTP inicia la API Debug en un puerto loopback libre con una clave aleatoria efímera y la conexión de pruebas. No utiliza ni modifica credenciales de producción. Apaga su proceso al terminar. Conserva datos **sintéticos** en la base desechable para inspección; una repetición debe usar otra base vacía. Incluye las comprobaciones de negocio más salud, JSON inválido, JWT inválido, ruta desconocida y estado HTTP 201 al crear tickets.

Desde `frontend/campusfix-web`:

```powershell
npm.cmd run build
npm.cmd test -- --watch=false
```

En el entorno usado, Vitest y PostgreSQL requirieron ejecución local fuera del sandbox para crear procesos. El aviso NuGet NU1900 impidió consultar vulnerabilidades en línea, no la compilación con dependencias existentes.

## Prueba de navegador local

`tests/CampusFix.Checks/browser-qa.mjs` inicia una API local y Edge headless mediante Playwright. Requiere el esquema QA creado y el permiso de creación por defecto del Solicitante. Utilizar una base sin restricciones de rol de otras pruebas: el modo HTTP cambia los permisos como parte de sus comprobaciones. La base que dejó el modo `--postgres` con rollback sirve para este recorrido.

Después de compilar Angular, copiar el contenido de `frontend/campusfix-web/dist/campusfix-web/browser/` a `backend/CampusFix.Api/wwwroot/` y compilar el backend Debug. La prueba crea una cuenta y un ticket sintéticos nuevos por ejecución y conserva sus datos. Ejecutar desde la raíz:

```powershell
# CAMPUSFIX_TEST_POSTGRES debe señalar la base QA local.
# CAMPUSFIX_PLAYWRIGHT puede indicar la ruta al paquete Playwright disponible.
node tests/CampusFix.Checks/browser-qa.mjs
```

El navegador predeterminado es Edge; `CAMPUSFIX_BROWSER_CHANNEL` permite usar otro canal instalado compatible con Playwright. El ejecutable rechaza hosts externos y nombres de base sin prefijo QA. El proceso API se cierra al terminar. No modifica el perfil personal del navegador.

Resultado final: 47 pruebas Angular, 62 comprobaciones de negocio, 66 de HTTP/PostgreSQL y 7 comprobaciones de navegador aprobadas. Las pruebas HTTP/PostgreSQL de la etapa 4 siguen siendo válidas porque no hubo cambios posteriores del backend. Se generó además `publish/` mediante `dotnet publish` como compilación local, sin despliegue remoto.

## Pendientes externos

- Desplegar los commits locales solo cuando el propietario lo autorice; este trabajo no ejecutó push ni despliegues.
- Después de un despliegue autorizado, comprobar creación con cuentas de prueba y correlacionar errores con su referencia en Render. Las respuestas de salud actuales no prueban que el arreglo esté publicado.
- No se verificaron envío/entrega real de correo, lector de pantalla ni recorridos visuales móviles de producción.
- El borrador de nuevo reporte usa sessionStorage separado por usuario; sobrevive a recarga/navegación de la pestaña, no garantiza recuperación después de cerrar la pestaña. No almacena adjuntos ni contraseñas.
- No se cambió la política general del módulo conocimiento para Solicitante ni se dividió el permiso analítica; el requisito aplicado oculta las guías relacionadas en tickets.
