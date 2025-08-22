# Generador de Interfaces TypeScript

Herramienta global de .NET para generar interfaces TypeScript a partir de proyectos C#. Convierte automáticamente clases y records de C# en interfaces TypeScript, creando un paquete npm listo para publicar.

## Características

- Genera interfaces TypeScript a partir de clases y records C#
- Detecta automáticamente proyectos en la solución
- Modo observador para regenerar interfaces cuando hay cambios
- Configuración mediante archivo JSON o línea de comandos
- Soporte para publicación automática del paquete npm
- Personalización completa del package.json (autor, licencia, repositorio)
- Detección automática de configuración desde package.json existente
- Soporte para scopes de npm y acceso público

## Instalación

### Como herramienta global de .NET

```bash
# Compilar e instalar desde el código fuente
dotnet pack -c Release
dotnet tool install --global --add-source ./bin/Release InterfacesGenerator

# O instalar directamente desde NuGet
dotnet tool install --global InterfacesGenerator
```

## Uso

### Uso básico

```bash
# Detecta automáticamente proyectos en el directorio actual
interfaces-generator

# Especificar proyecto manualmente
interfaces-generator --project C:\ruta\al\proyecto

# Generar y publicar el paquete npm
interfaces-generator --publish
```

### Opciones disponibles

```
Opciones:
  --project, -p         Ruta al proyecto C# (detectado automáticamente si no se especifica)
  --output, -o          Ruta de salida para el proyecto npm
  --package-name, -n    Nombre del paquete npm
  --version, -v         Versión del paquete npm
  --repository, -r      URL del repositorio git
  --author, -a          Autor del paquete
  --license, -l         Licencia del paquete (por defecto: ISC)
  --publish, --pub      Publicar el paquete npm automáticamente
  --auto-login, -al     Iniciar sesión automáticamente en npm
  --npm-scope, -ns      Alcance del paquete npm
  --watch, -w           Modo observador: regenerar cuando se detecten cambios
  --config, -c          Ruta al archivo de configuración JSON
  --save-config         Guardar la configuración actual en un archivo JSON
  --help, -h            Mostrar esta ayuda
```

### Uso con archivo de configuración

Puedes guardar tu configuración en un archivo JSON:

```bash
# Guardar configuración actual
interfaces-generator --project C:\ruta\al\proyecto --package-name mi-paquete --save-config config.json

# Usar configuración guardada
interfaces-generator --config config.json
```

Ejemplo de archivo de configuración:

```json
{
  "projectPath": "C:\\ruta\\al\\proyecto",
  "outputPath": "C:\\ruta\\salida",
  "packageName": "mi-paquete",
  "version": "1.0.0",
  "repository": "https://github.com/usuario/repo",
  "author": "Tu Nombre <tu@email.com>",
  "license": "MIT",
  "publish": false,
  "watch": false,
  "autoLogin": false,
  "npmScope": ""
}
```

### Publicación en npm

Para publicar automáticamente el paquete generado en npm:

```bash
# Publicar con configuración básica
interfaces-generator --publish

# Publicar con scope de npm
interfaces-generator --publish --npm-scope @mi-organizacion

# Publicar con verificación de autenticación
interfaces-generator --publish --auto-login true
```

Requisitos para publicar:
- Node.js y npm instalados y en el PATH del sistema
- Autenticación en npm (`npm login` o archivo .npmrc configurado)

## Detección automática de configuración

La herramienta puede detectar automáticamente valores de configuración desde:
- Un package.json existente en el proyecto o directorios superiores
- Archivos de proyecto C# (.csproj)

Esto minimiza la configuración manual necesaria.

## Integración con IDEs

### Visual Studio

1. **Terminal integrada**:
   - Abre la terminal integrada (Ver > Terminal)
   - Ejecuta `interfaces-generator`

2. **Tareas externas**:
   - Ve a Herramientas > Opciones > Proyectos y Soluciones > Tareas Externas
   - Agrega una nueva tarea con el comando `interfaces-generator`
   - Configura los argumentos según necesites

### Rider

1. **Terminal integrado**:
   - Abre la terminal (Alt+F12)
   - Ejecuta `interfaces-generator`

2. **External Tools**:
   - Ve a Settings > Tools > External Tools
   - Agrega una nueva herramienta con el programa `interfaces-generator`
   - Configura los argumentos según necesites

## Solución de problemas

### Error al publicar el paquete npm

Si encuentras errores al publicar el paquete npm:

1. Verifica que Node.js y npm estén instalados y en el PATH del sistema
2. Ejecuta `npm --version` para confirmar que npm está disponible
3. Asegúrate de estar autenticado en npm con `npm login`
4. Verifica que el directorio de salida existe y es accesible
5. Comprueba que no hay errores en el package.json generado

## Desinstalación

```bash
dotnet tool uninstall --global InterfacesGenerator
