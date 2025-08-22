using System.Diagnostics;

namespace InterfacesGenerator;

public static class NpmPublisher
{
    public static async Task PublishNpmPackage(string outputDir, bool autoLogin = false, string scope = "")
    {
        Console.WriteLine($"Publicando paquete npm desde el directorio: {outputDir}");
        Console.WriteLine($"Directorio actual: {Directory.GetCurrentDirectory()}");

        try
        {
            // Asegurarse de que la ruta sea absoluta
            outputDir = Path.GetFullPath(outputDir);
            
            // Verificar que el directorio de salida existe y es accesible
            if (!Directory.Exists(outputDir))
            {
                Console.WriteLine($"Error: El directorio de salida '{outputDir}' no existe.");
                return;
            }

            // Verificar que el directorio de salida es accesible
            try
            {
                var testFile = Path.Combine(outputDir, "test.txt");
                await File.WriteAllTextAsync(testFile, "Test");
                File.Delete(testFile);
                Console.WriteLine($"Directorio de salida '{outputDir}' es accesible.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: No se puede acceder al directorio de salida '{outputDir}': {ex.Message}");
                return;
            }

            // Buscar npm en diferentes ubicaciones
            var npmPath = FindNpmExecutable();
            if (string.IsNullOrEmpty(npmPath))
            {
                Console.WriteLine("Error: npm no está instalado o no está en el PATH del sistema.");
                Console.WriteLine("Por favor, instale Node.js desde https://nodejs.org/");
                Console.WriteLine("Después de la instalación, reinicie su terminal o IDE para que los cambios en el PATH surtan efecto.");
                return;
            }

            Console.WriteLine($"Usando npm desde: {npmPath}");

            // Verificar que npm está instalado
            var npmVersionProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = npmPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = outputDir // Asegurarse de usar el directorio correcto
                }
            };

            try
            {
                npmVersionProcess.Start();
                await npmVersionProcess.WaitForExitAsync();
                var npmVersion = await npmVersionProcess.StandardOutput.ReadToEndAsync();
                Console.WriteLine($"Versión de npm: {npmVersion.Trim()}");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.WriteLine($"Error al ejecutar npm: {ex.Message}");
                Console.WriteLine("Por favor, verifique que npm está correctamente instalado y configurado.");
                return;
            }

            if (npmVersionProcess.ExitCode != 0)
            {
                var error = await npmVersionProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"Error al ejecutar npm: {error}");
                Console.WriteLine("Por favor, verifique que npm está correctamente instalado y configurado.");
                return;
            }

            // Verificar autenticación en npm si es necesario
            if (autoLogin)
            {
                Console.WriteLine("Verificando autenticación en npm...");
                var npmWhoamiProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = npmPath,
                        Arguments = "whoami",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = outputDir // Asegurarse de usar el directorio correcto
                    }
                };

                try
                {
                    npmWhoamiProcess.Start();
                    await npmWhoamiProcess.WaitForExitAsync();
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    Console.WriteLine($"Error al verificar la autenticación en npm: {ex.Message}");
                    return;
                }

                if (npmWhoamiProcess.ExitCode != 0)
                {
                    Console.WriteLine("No se ha detectado una sesión activa en npm. Por favor, ejecute 'npm login' antes de publicar.");
                    Console.WriteLine("Alternativamente, puede crear un archivo .npmrc en su directorio de usuario con un token de acceso.");
                    return;
                }
                else
                {
                    var username = await npmWhoamiProcess.StandardOutput.ReadToEndAsync();
                    Console.WriteLine($"Publicando como usuario npm: {username.Trim()}");
                }
            }

            // Verificar que package.json existe en el directorio de salida
            var packageJsonPath = Path.Combine(outputDir, "package.json");
            if (!File.Exists(packageJsonPath))
            {
                Console.WriteLine($"Error: No se encontró el archivo package.json en '{outputDir}'.");
                return;
            }

            // Instalar dependencias
            Console.WriteLine("Instalando dependencias npm...");
            var npmInstallProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = npmPath,
                    Arguments = "install",
                    WorkingDirectory = outputDir, // Asegurarse de usar el directorio correcto
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                npmInstallProcess.Start();
                await npmInstallProcess.WaitForExitAsync();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.WriteLine($"Error al instalar dependencias npm: {ex.Message}");
                return;
            }

            if (npmInstallProcess.ExitCode != 0)
            {
                var error = await npmInstallProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"Error al instalar dependencias npm: {error}");
                return;
            }

            // Compilar el proyecto TypeScript
            Console.WriteLine("Compilando proyecto TypeScript...");
            var npmBuildProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = npmPath,
                    Arguments = "run build",
                    WorkingDirectory = outputDir, // Asegurarse de usar el directorio correcto
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                npmBuildProcess.Start();
                await npmBuildProcess.WaitForExitAsync();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.WriteLine($"Error al compilar el proyecto TypeScript: {ex.Message}");
                return;
            }

            if (npmBuildProcess.ExitCode != 0)
            {
                var error = await npmBuildProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"Error al compilar el proyecto TypeScript: {error}");
                return;
            }

            // Publicar el paquete npm
            Console.WriteLine("Publicando paquete npm...");
            var publishArgs = "publish";
            
            // Agregar --access=public si se especifica un scope
            if (!string.IsNullOrEmpty(scope))
            {
                publishArgs += " --access=public";
            }
            
            var npmPublishProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = npmPath,
                    Arguments = publishArgs,
                    WorkingDirectory = outputDir, // Asegurarse de usar el directorio correcto
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                npmPublishProcess.Start();
                await npmPublishProcess.WaitForExitAsync();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.WriteLine($"Error al publicar el paquete npm: {ex.Message}");
                return;
            }

            if (npmPublishProcess.ExitCode != 0)
            {
                var error = await npmPublishProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"Error al publicar el paquete npm: {error}");
                return;
            }

            var output = await npmPublishProcess.StandardOutput.ReadToEndAsync();
            Console.WriteLine("Paquete npm publicado correctamente:");
            Console.WriteLine(output);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al publicar el paquete npm: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static string FindNpmExecutable()
    {
        // En Windows, siempre usar npm.cmd en lugar de npm
        if (Environment.OSVersion.Platform != PlatformID.Win32NT) return "npm";
        // Buscar en ubicaciones comunes de instalación de Node.js
        string[] commonPaths =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "npm.cmd"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "nodejs", "npm.cmd"),
            Path.Combine(Environment.GetEnvironmentVariable("APPDATA") ?? "", "npm", "npm.cmd"),
            Path.Combine(Environment.GetEnvironmentVariable("APPDATA") ?? "", "Roaming", "npm", "npm.cmd")
        ];
            
        foreach (var path in commonPaths)
        {
            if (!File.Exists(path)) continue;
            Console.WriteLine($"Encontrado npm.cmd en: {path}");
            return path;
        }
            
        // Si no se encuentra en ubicaciones comunes, intentar con npm.cmd en el PATH
        return "npm.cmd";

        // En otros sistemas operativos, usar npm
    }
}
