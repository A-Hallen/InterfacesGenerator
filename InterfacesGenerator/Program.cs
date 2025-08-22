using System.Text.Json;
using System.Text.Json.Serialization;

namespace InterfacesGenerator;

static class Program
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
                ConfigFile = string.Empty,
                Repository = string.Empty,
                Author = string.Empty,
                License = "ISC",
                AutoLogin = false,
                NpmScope = string.Empty
            };

            // Procesar argumentos de línea de comandos
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLower();

                switch (arg)
                {
                    case "--project" or "-p" when i + 1 < args.Length:
                        config.ProjectPath = args[++i];
                        break;
                    case "--output" or "-o" when i + 1 < args.Length:
                        config.OutputPath = args[++i];
                        break;
                    case "--package-name" or "-n" when i + 1 < args.Length:
                        config.PackageName = args[++i];
                        break;
                    case "--version" or "-v" when i + 1 < args.Length:
                        config.Version = args[++i];
                        break;
                    case "--repository" or "-r" when i + 1 < args.Length:
                        config.Repository = args[++i];
                        break;
                    case "--author" or "-a" when i + 1 < args.Length:
                        config.Author = args[++i];
                        break;
                    case "--license" or "-l" when i + 1 < args.Length:
                        config.License = args[++i];
                        break;
                    case "--publish":
                    case "--pub":
                        config.Publish = true;
                        break;
                    case "--watch":
                    case "-w":
                        config.Watch = true;
                        break;
                    case "--config" or "-c" when i + 1 < args.Length:
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
                                    if (string.IsNullOrEmpty(config.Repository)) config.Repository = fileConfig.Repository;
                                    if (string.IsNullOrEmpty(config.Author)) config.Author = fileConfig.Author;
                                    if (config.License == "ISC") config.License = fileConfig.License;
                                    if (!config.AutoLogin) config.AutoLogin = fileConfig.AutoLogin;
                                    if (string.IsNullOrEmpty(config.NpmScope)) config.NpmScope = fileConfig.NpmScope;
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

                        break;
                    }
                    case "--help":
                    case "-h":
                        ShowHelp();
                        return 0;
                    case "--save-config" when i + 1 < args.Length:
                    {
                        var configPath = args[++i];
                        SaveConfig(config, configPath);
                        Console.WriteLine($"Configuración guardada en {configPath}");
                        return 0;
                    }
                    case "--auto-login" or "-al" when i + 1 < args.Length:
                        config.AutoLogin = bool.Parse(args[++i]);
                        break;
                    case "--npm-scope" or "-ns" when i + 1 < args.Length:
                        config.NpmScope = args[++i];
                        break;
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

            // Intentar cargar valores predeterminados de package.json
            config = await TryLoadPackageJsonDefaults(config);

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
                    config.Publish,
                    config.Repository,
                    config.Author,
                    config.License,
                    config.AutoLogin,
                    config.NpmScope);
                
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
        Console.WriteLine("  --repository, -r     URL del repositorio git");
        Console.WriteLine("  --author, -a         Autor del paquete");
        Console.WriteLine("  --license, -l        Licencia del paquete (por defecto: ISC)");
        Console.WriteLine("  --publish, --pub     Publicar el paquete npm automáticamente");
        Console.WriteLine("  --watch, -w          Modo observador: regenerar cuando se detecten cambios");
        Console.WriteLine("  --config, -c         Ruta al archivo de configuración JSON");
        Console.WriteLine("  --save-config        Guardar la configuración actual en un archivo JSON");
        Console.WriteLine("  --help, -h           Mostrar esta ayuda");
        Console.WriteLine("  --auto-login, -al    Iniciar sesión automáticamente en npm (requiere --npm-scope)");
        Console.WriteLine("  --npm-scope, -ns     Alcance del paquete npm");
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

    private static async Task<GeneratorConfig> TryLoadPackageJsonDefaults(GeneratorConfig config)
    {
        try
        {
            // Buscar package.json en el directorio del proyecto o en directorios superiores
            string? directory = Path.GetDirectoryName(config.ProjectPath);
            string? packageJsonPath = null;
            
            while (!string.IsNullOrEmpty(directory))
            {
                string potentialPath = Path.Combine(directory, "package.json");
                if (File.Exists(potentialPath))
                {
                    packageJsonPath = potentialPath;
                    break;
                }
                directory = Path.GetDirectoryName(directory);
            }
            
            // Si no se encontró en directorios superiores, buscar en subdirectorios
            if (packageJsonPath == null && Directory.Exists(config.ProjectPath))
            {
                var packageJsonFiles = Directory.GetFiles(config.ProjectPath, "package.json", SearchOption.AllDirectories);
                if (packageJsonFiles.Length > 0)
                {
                    packageJsonPath = packageJsonFiles[0]; // Usar el primer package.json encontrado
                }
            }
            
            if (packageJsonPath != null)
            {
                Console.WriteLine($"Encontrado package.json en {packageJsonPath}. Usando valores predeterminados...");
                
                var packageJsonContent = await File.ReadAllTextAsync(packageJsonPath);
                using var jsonDoc = JsonDocument.Parse(packageJsonContent);
                var root = jsonDoc.RootElement;
                
                // Extraer valores si existen y no están ya establecidos
                if (root.TryGetProperty("name", out var nameElement) && config.PackageName == "interfaces-mensajeria")
                {
                    config.PackageName = nameElement.GetString() ?? config.PackageName;
                }
                
                if (root.TryGetProperty("version", out var versionElement) && config.Version == "1.0.0")
                {
                    config.Version = versionElement.GetString() ?? config.Version;
                }
                
                if (root.TryGetProperty("author", out var authorElement) && string.IsNullOrEmpty(config.Author))
                {
                    config.Author = authorElement.ValueKind == JsonValueKind.String 
                        ? authorElement.GetString() ?? string.Empty 
                        : string.Empty;
                }
                
                if (root.TryGetProperty("license", out var licenseElement) && config.License == "ISC")
                {
                    config.License = licenseElement.GetString() ?? config.License;
                }
                
                if (root.TryGetProperty("repository", out var repoElement) && string.IsNullOrEmpty(config.Repository))
                {
                    if (repoElement.ValueKind == JsonValueKind.String)
                    {
                        config.Repository = repoElement.GetString() ?? string.Empty;
                    }
                    else if (repoElement.ValueKind == JsonValueKind.Object && 
                             repoElement.TryGetProperty("url", out var urlElement))
                    {
                        config.Repository = urlElement.GetString() ?? string.Empty;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al leer package.json: {ex.Message}");
        }
        
        return config;
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
            config.Publish,
            config.Repository,
            config.Author,
            config.License,
            config.AutoLogin,
            config.NpmScope);
        
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
                    config.Publish,
                    config.Repository,
                    config.Author,
                    config.License,
                    config.AutoLogin,
                    config.NpmScope);
                
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
    public string Repository { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string License { get; set; } = "ISC";
    public bool AutoLogin { get; set; } = false;
    public string NpmScope { get; set; } = string.Empty;
    
    [JsonIgnore]
    public string ConfigFile { get; set; } = string.Empty;
}