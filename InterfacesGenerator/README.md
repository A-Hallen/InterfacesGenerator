# Generador de Interfaces TypeScript

Herramienta global de .NET para generar interfaces TypeScript a partir de proyectos C#.

## Características

- Genera interfaces TypeScript a partir de clases y records C#
- Detecta automáticamente proyectos en la solución
- Modo observador para regenerar interfaces cuando hay cambios
- Configuración mediante archivo JSON o línea de comandos
- Soporte para publicación automática del paquete npm

## Instalación

```bash
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
  --publish, --pub      Publicar el paquete npm automáticamente
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
  "publish": false,
  "watch": false
}
```

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

## Desinstalación

```bash
dotnet tool uninstall --global InterfacesGenerator
```
