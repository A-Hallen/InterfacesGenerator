using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GeneradorInterfaces;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            // Configuración predeterminada
            var config = new GeneratorConfig
            {
                ProjectPath = string.Empty,
                OutputPath = Path.Combine(Directory.GetCurrentDirectory(), "interfaces-mensajeria"),
                PackageName = "interfaces-mensajeria",
                Version = "1.0.0",
                Publish = false,
                Watch = false,
                ConfigFile = string.Empty
            };

            // Procesar argumentos de línea de comandos
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                
                if ((arg == "--project" || arg == "-p") && i + 1 < args.Length)
                {
                    config.ProjectPath = args[++i];
                }
                else if ((arg == "--output" || arg == "-o") && i + 1 < args.Length)
                {
                    config.OutputPath = args[++i];
                }
                else if ((arg == "--package-name" || arg == "-n") && i + 1 < args.Length)
                {
                    config.PackageName = args[++i];
                }
                else if ((arg == "--version" || arg == "-v") && i + 1 < args.Length)
                {
                    config.Version = args[++i];
                }
                else if (arg == "--publish" || arg == "--pub")
                {
                    config.Publish = true;
                }
                else if (arg == "--watch" || arg == "-w")
                {
                    config.Watch = true;
                }
                else if ((arg == "--config" || arg == "-c") && i + 1 < args.Length)
                {
                    config.ConfigFile = args[++i];
                    if (File.Exists(config.ConfigFile))
                    {
                        try
                        {
                            var fileConfig = JsonSerializer.Deserialize<GeneratorConfig>(
                                File.ReadAllText(config.ConfigFile),
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            
                            if (fileConfig != null)
                            {
                                // Solo sobrescribir propiedades que no se hayan especificado en la línea de comandos
                                if (string.IsNullOrEmpty(config.ProjectPath)) config.ProjectPath = fileConfig.ProjectPath;
                                if (config.OutputPath == Path.Combine(Directory.GetCurrentDirectory(), "interfaces-mensajeria")) 
                                    config.OutputPath = fileConfig.OutputPath;
                                if (config.PackageName == "interfaces-mensajeria") config.PackageName = fileConfig.PackageName;
                                if (config.Version == "1.0.0") config.Version = fileConfig.Version;
                                if (!config.Publish) config.Publish = fileConfig.Publish;
                                if (!config.Watch) config.Watch = fileConfig.Watch;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al leer el archivo de configuración: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"El archivo de configuración {config.ConfigFile} no existe.");
                    }
                }
                else if (arg == "--help" || arg == "-h")
                {
                    ShowHelp();
                    return 0;
                }
                else if (arg == "--save-config" && i + 1 < args.Length)
                {
                    var configPath = args[++i];
                    SaveConfig(config, configPath);
                    Console.WriteLine($"Configuración guardada en {configPath}");
                    return 0;
                }
            }

            // Si no se especificó un proyecto, intentar detectar automáticamente
            if (string.IsNullOrEmpty(config.ProjectPath))
            {
                config.ProjectPath = await DetectProjectAsync();
                if (string.IsNullOrEmpty(config.ProjectPath))
                {
                    Console.WriteLine("No se pudo detectar automáticamente un proyecto. Por favor, especifique la ruta con --project.");
                    ShowHelp();
                    return 1;
                }
            }

            // Verificar que el proyecto existe
            if (!Directory.Exists(config.ProjectPath) && !File.Exists(config.ProjectPath))
            {
                Console.WriteLine($"El proyecto especificado no existe: {config.ProjectPath}");
                return 1;
            }

            // Ejecutar el generador
            if (config.Watch)
            {
                await WatchAndGenerateAsync(config);
                return 0;
            }
            else
            {
                await TypeScriptGenerator.GenerateTypeScriptInterfaces(
                    config.ProjectPath, 
                    config.OutputPath, 
                    config.PackageName, 
                    config.Version, 
                    config.Publish);
                
                Console.WriteLine("Interfaces TypeScript generadas correctamente.");
                return 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar interfaces TypeScript: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Generador de interfaces TypeScript para proyectos C#");
        Console.WriteLine("Uso: interfaces-generator [opciones]");
        Console.WriteLine();
        Console.WriteLine("Opciones:");
        Console.WriteLine("  --project, -p         Ruta al proyecto C# (detectado automáticamente si no se especifica)");
        Console.WriteLine("  --output, -o         Ruta de salida para el proyecto npm");
        Console.WriteLine("  --package-name, -n   Nombre del paquete npm");
        Console.WriteLine("  --version, -v        Versión del paquete npm");
        Console.WriteLine("  --publish, --pub     Publicar el paquete npm automáticamente");
        Console.WriteLine("  --watch, -w          Modo observador: regenerar cuando se detecten cambios");
        Console.WriteLine("  --config, -c         Ruta al archivo de configuración JSON");
        Console.WriteLine("  --save-config        Guardar la configuración actual en un archivo JSON");
        Console.WriteLine("  --help, -h           Mostrar esta ayuda");
    }

    private static void SaveConfig(GeneratorConfig config, string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        File.WriteAllText(path, JsonSerializer.Serialize(config, options));
    }

    private static async Task<string> DetectProjectAsync()
    {
        // Buscar archivos .csproj en el directorio actual y subdirectorios
        var currentDir = Directory.GetCurrentDirectory();
        var projectFiles = Directory.GetFiles(currentDir, "*.csproj", SearchOption.AllDirectories);
        
        if (projectFiles.Length == 0)
        {
            // Buscar soluciones .sln y luego proyectos dentro de ellas
            var solutionFiles = Directory.GetFiles(currentDir, "*.sln", SearchOption.AllDirectories);
            if (solutionFiles.Length > 0)
            {
                // Tomar la primera solución encontrada
                var solutionDir = Path.GetDirectoryName(solutionFiles[0]);
                if (solutionDir != null)
                {
                    projectFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);
                }
            }
        }

        // Si encontramos proyectos, buscar uno que parezca contener mensajería
        if (projectFiles.Length > 0)
        {
            // Primero buscar proyectos que contengan "Mensajeria" en el nombre
            var mensajeriaProject = projectFiles.FirstOrDefault(p => 
                Path.GetFileNameWithoutExtension(p).Contains("Mensajeria", StringComparison.OrdinalIgnoreCase));
            
            if (!string.IsNullOrEmpty(mensajeriaProject))
            {
                return Path.GetDirectoryName(mensajeriaProject);
            }
            
            // Si no encontramos uno específico, usar el primer proyecto
            return Path.GetDirectoryName(projectFiles[0]);
        }
        
        return string.Empty;
    }

    private static async Task WatchAndGenerateAsync(GeneratorConfig config)
    {
        Console.WriteLine($"Modo observador activado. Monitoreando cambios en {config.ProjectPath}");
        Console.WriteLine("Presione Ctrl+C para detener.");
        
        // Generar interfaces inicialmente
        await TypeScriptGenerator.GenerateTypeScriptInterfaces(
            config.ProjectPath, 
            config.OutputPath, 
            config.PackageName, 
            config.Version, 
            config.Publish);
        
        Console.WriteLine("Interfaces TypeScript generadas correctamente. Esperando cambios...");
        
        // Configurar FileSystemWatcher
        using var watcher = new FileSystemWatcher
        {
            Path = config.ProjectPath,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = "*.cs",
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        
        // Usar un temporizador para evitar múltiples regeneraciones cuando se modifican varios archivos
        var timer = new System.Timers.Timer(2000); // 2 segundos de debounce
        timer.AutoReset = false;
        timer.Elapsed += async (sender, e) =>
        {
            Console.WriteLine("Cambios detectados. Regenerando interfaces...");
            try
            {
                await TypeScriptGenerator.GenerateTypeScriptInterfaces(
                    config.ProjectPath, 
                    config.OutputPath, 
                    config.PackageName, 
                    config.Version, 
                    config.Publish);
                
                Console.WriteLine("Interfaces TypeScript regeneradas correctamente. Esperando cambios...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al regenerar interfaces: {ex.Message}");
            }
        };
        
        // Eventos para detectar cambios
        watcher.Changed += (sender, e) => RestartTimer(timer);
        watcher.Created += (sender, e) => RestartTimer(timer);
        watcher.Deleted += (sender, e) => RestartTimer(timer);
        watcher.Renamed += (sender, e) => RestartTimer(timer);
        
        // Mantener la aplicación en ejecución
        var tcs = new TaskCompletionSource<bool>();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            tcs.SetResult(true);
        };
        
        await tcs.Task;
    }
    
    private static void RestartTimer(System.Timers.Timer timer)
    {
        timer.Stop();
        timer.Start();
    }
}

public class GeneratorConfig
{
    public string ProjectPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool Publish { get; set; }
    public bool Watch { get; set; }
    
    [JsonIgnore]
    public string ConfigFile { get; set; } = string.Empty;
}