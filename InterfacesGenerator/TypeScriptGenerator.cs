using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GeneradorInterfaces;

public static class TypeScriptGenerator
{
    public static async Task GenerateTypeScriptInterfaces(string sourceDir, string outputDir, string packageName, string version, bool publish)
    {
        try
        {
            var csFiles = Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories);
            Console.WriteLine($"Encontrados {csFiles.Length} archivos .cs");

            Directory.CreateDirectory(outputDir);
            Directory.CreateDirectory(Path.Combine(outputDir, "src"));
            Directory.CreateDirectory(Path.Combine(outputDir, "docs"));

            var typeScriptInterfaces = new Dictionary<string, List<string>>();
            var imports = new Dictionary<string, HashSet<string>>();

            foreach (var csFile in csFiles)
            {
                var relativePath = Path.GetRelativePath(sourceDir, csFile);
                var directory = Path.GetDirectoryName(relativePath) ?? string.Empty;
                var tsDirectory = directory.ToLowerInvariant();

                if (!typeScriptInterfaces.ContainsKey(tsDirectory))
                {
                    typeScriptInterfaces[tsDirectory] = new List<string>();
                    imports[tsDirectory] = new HashSet<string>();
                }

                var fileContent = await File.ReadAllTextAsync(csFile);
                var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
                var root = syntaxTree.GetCompilationUnitRoot();

                ProcessRecordsAndClasses(root, typeScriptInterfaces[tsDirectory], imports[tsDirectory], tsDirectory);
            }

            foreach (var dir in typeScriptInterfaces.Keys)
            {
                var tsDir = Path.Combine(outputDir, "src", dir);
                Directory.CreateDirectory(tsDir);

                var fileName = Path.GetFileName(dir) + ".ts";
                var filePath = Path.Combine(tsDir, fileName);

                var sb = new StringBuilder();

                if (imports[dir].Count > 0)
                {
                    foreach (var import in imports[dir])
                    {
                        sb.AppendLine(import);
                    }
                    sb.AppendLine();
                }

                foreach (var tsInterface in typeScriptInterfaces[dir])
                {
                    sb.AppendLine(tsInterface);
                    sb.AppendLine();
                }

                await File.WriteAllTextAsync(filePath, sb.ToString());
                Console.WriteLine($"Generado archivo: {filePath}");
            }

            await GenerateIndexFile(outputDir, typeScriptInterfaces.Keys);
            await GeneratePackageJson(outputDir, packageName, version);
            await GenerateTsConfigJson(outputDir);
            await GenerateReadme(outputDir, packageName);
            await GenerateProcesoMd(outputDir);

            if (publish)
            {
                await NpmPublisher.PublishNpmPackage(outputDir);
            }

            Console.WriteLine("Generación de interfaces TypeScript completada con éxito.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar interfaces TypeScript: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static void ProcessRecordsAndClasses(CompilationUnitSyntax root, List<string> interfaces, HashSet<string> imports, string currentDirectory)
    {
        string namespaceName = "Mensajeria";
        
        var fileScopedNamespace = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScopedNamespace != null)
        {
            namespaceName = fileScopedNamespace.Name.ToString();
        }
        else
        {
            var namespaceDeclaration = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if (namespaceDeclaration != null)
            {
                namespaceName = namespaceDeclaration.Name.ToString();
            }
        }

        foreach (var recordDeclaration in root.DescendantNodes().OfType<RecordDeclarationSyntax>())
        {
            var recordName = recordDeclaration.Identifier.Text;
            var tsInterface = GenerateTypeScriptInterfaceFromRecord(recordDeclaration, imports, currentDirectory);
            interfaces.Add(tsInterface);
        }

        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var className = classDeclaration.Identifier.Text;
            var tsInterface = GenerateTypeScriptInterfaceFromClass(classDeclaration, imports, currentDirectory);
            interfaces.Add(tsInterface);
        }
    }

    private static string GenerateTypeScriptInterfaceFromRecord(RecordDeclarationSyntax recordDeclaration, HashSet<string> imports, string currentDirectory)
    {
        var recordName = recordDeclaration.Identifier.Text;
        var sb = new StringBuilder();

        sb.AppendLine($"export interface {recordName} {{");

        if (recordDeclaration.ParameterList != null)
        {
            foreach (var parameter in recordDeclaration.ParameterList.Parameters)
            {
                var paramName = parameter.Identifier.Text;
                var paramType = parameter.Type?.ToString() ?? "any";
                var tsType = TypeMapper.MapCSharpTypeToTypeScript(paramType, imports, currentDirectory);

                sb.AppendLine($"  {TypeMapper.CamelCase(paramName)}: {tsType};");
            }
        }

        foreach (var property in recordDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            var propName = property.Identifier.Text;
            var propType = property.Type.ToString();
            var tsType = TypeMapper.MapCSharpTypeToTypeScript(propType, imports, currentDirectory);

            sb.AppendLine($"  {TypeMapper.CamelCase(propName)}: {tsType};");
        }

        sb.Append("}");
        return sb.ToString();
    }

    private static string GenerateTypeScriptInterfaceFromClass(ClassDeclarationSyntax classDeclaration, HashSet<string> imports, string currentDirectory)
    {
        var className = classDeclaration.Identifier.Text;
        var sb = new StringBuilder();

        sb.AppendLine($"export interface {className} {{");

        var constructor = classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();

        foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            var propName = property.Identifier.Text;
            var propType = property.Type.ToString();
            var tsType = TypeMapper.MapCSharpTypeToTypeScript(propType, imports, currentDirectory);

            sb.AppendLine($"  {TypeMapper.CamelCase(propName)}: {tsType};");
        }

        sb.Append("}");
        return sb.ToString();
    }

    private static async Task GenerateIndexFile(string outputDir, IEnumerable<string> directories)
    {
        var sb = new StringBuilder();

        foreach (var dir in directories)
        {
            var fileName = Path.GetFileName(dir);
            sb.AppendLine($"export * from './src/{dir}/{fileName}';");
        }

        await File.WriteAllTextAsync(Path.Combine(outputDir, "src", "index.ts"), sb.ToString());
        Console.WriteLine("Generado archivo index.ts");
    }

    private static async Task GeneratePackageJson(string outputDir, string packageName, string version)
    {
        var packageJson = @$"{{
  ""name"": ""{packageName}"",
  ""version"": ""{version}"",
  ""description"": ""Interfaces TypeScript para el proyecto de Mensajería"",
  ""main"": ""dist/index.js"",
  ""types"": ""dist/index.d.ts"",
  ""scripts"": {{
    ""build"": ""tsc"",
    ""prepare"": ""npm run build""
  }},
  ""keywords"": [
    ""typescript"",
    ""interfaces"",
    ""mensajeria""
  ],
  ""author"": """",
  ""license"": ""ISC"",
  ""devDependencies"": {{
    ""typescript"": ""^5.0.0""
  }}
}}";

        await File.WriteAllTextAsync(Path.Combine(outputDir, "package.json"), packageJson);
        Console.WriteLine("Generado archivo package.json");
    }

    private static async Task GenerateTsConfigJson(string outputDir)
    {
        var tsConfigJson = @"{
  ""compilerOptions"": {
    ""target"": ""es2016"",
    ""module"": ""commonjs"",
    ""declaration"": true,
    ""outDir"": ""./dist"",
    ""esModuleInterop"": true,
    ""forceConsistentCasingInFileNames"": true,
    ""strict"": true,
    ""skipLibCheck"": true
  },
  ""include"": [""src/**/*""],
  ""exclude"": [""node_modules"", ""**/*.test.ts""]
}";

        await File.WriteAllTextAsync(Path.Combine(outputDir, "tsconfig.json"), tsConfigJson);
        Console.WriteLine("Generado archivo tsconfig.json");
    }

    private static async Task GenerateReadme(string outputDir, string packageName)
    {
        var readme = @$"# {packageName}

Interfaces TypeScript para los mensajes utilizados en el sistema Business Place.

## Instalación

```bash
npm install {packageName}
```

## Uso

```typescript
import {{ ObtenerOrdenRequest, ObtenerOrdenResponse }} from '{packageName}';

// Ejemplo de uso en una llamada API
const request: ObtenerOrdenRequest = {{ id: 123 }};
const response = await api.post<ObtenerOrdenResponse>('/api/orden/obtener', request);
```

## Documentación

Para más información sobre el proceso de generación de estas interfaces, consulta [PROCESO.md](./docs/PROCESO.md).

Para ejemplos de uso, consulta [EJEMPLOS.md](./docs/EJEMPLOS.md).
";

        await File.WriteAllTextAsync(Path.Combine(outputDir, "README.md"), readme);
        Console.WriteLine("Generado archivo README.md");
    }

    private static async Task GenerateProcesoMd(string outputDir)
    {
        var procesoMd = @"# Proceso de Generación de Interfaces TypeScript

Este documento describe el proceso de generación de interfaces TypeScript a partir de las clases y records de C# del proyecto de Mensajería.

## Análisis del Proyecto C#

El proyecto de Mensajería contiene:

1. Records de C# que representan solicitudes (requests) que implementan `IRequest<T>` de MediatR
2. Clases de C# que representan respuestas (responses)
3. Clases de C# que representan modelos de datos
4. Todos estos tipos están en el namespace ""Mensajeria""

## Estrategia de Conversión

### Mapeo de Tipos de C# a TypeScript

| C# | TypeScript |
|----|------------|
| string | string |
| int | number |
| decimal | number |
| bool | boolean |
| DateTime | string (formato ISO) |
| T[] | T[] |
| T? | T \| null |
| Dictionary<K, V> | Record<K, V> |

### Reglas de Conversión

1. **Records de C# a interfaces TypeScript**:
   ```typescript
   // C#: public record ObtenerOrdenRequest(int Id) : IRequest<ObtenerOrdenResponse> { }
   
   export interface ObtenerOrdenRequest {
     id: number;
   }
   ```

2. **Clases de C# a interfaces TypeScript**:
   ```typescript
   // C#: public class ObtenerOrdenResponse(int id, string cliente) { ... }
   
   export interface ObtenerOrdenResponse {
     id: number;
     cliente: string;
   }
   ```

## Proceso de Actualización

Para actualizar las interfaces cuando cambie el proyecto de Mensajería, sigue estos pasos:

1. Ejecuta el generador con el comando:
   ```
   dotnet run --project GeneradorInterfaces
   ```

2. Opcionalmente, puedes especificar opciones adicionales:
   ```
   dotnet run --project GeneradorInterfaces -- --source <ruta-fuente> --output <ruta-salida> --package-name <nombre-paquete> --version <version> --publish
   ```

3. Publica el paquete npm:
   ```
   cd interfaces-mensajeria
   npm publish
   ```
";

        await File.WriteAllTextAsync(Path.Combine(outputDir, "docs", "PROCESO.md"), procesoMd);
        Console.WriteLine("Generado archivo PROCESO.md");

        var ejemplosMd = @"# Ejemplos de Uso

Este documento proporciona ejemplos de cómo utilizar las interfaces TypeScript generadas en diferentes escenarios.

## Uso con Axios

```typescript
import axios from 'axios';
import { ObtenerOrdenRequest, ObtenerOrdenResponse } from 'interfaces-mensajeria';

async function obtenerOrden(id: number): Promise<ObtenerOrdenResponse> {
  const request: ObtenerOrdenRequest = { id };
  const response = await axios.post<ObtenerOrdenResponse>('/api/orden/obtener', request);
  return response.data;
}
```

## Uso con React Query

```typescript
import { useQuery } from 'react-query';
import axios from 'axios';
import { ObtenerOrdenRequest, ObtenerOrdenResponse } from 'interfaces-mensajeria';

function useOrden(id: number) {
  return useQuery(['orden', id], async () => {
    const request: ObtenerOrdenRequest = { id };
    const response = await axios.post<ObtenerOrdenResponse>('/api/orden/obtener', request);
    return response.data;
  });
}

// En un componente
function OrdenComponent({ id }: { id: number }) {
  const { data, isLoading, error } = useOrden(id);
  
  if (isLoading) return <div>Cargando...</div>;
  if (error) return <div>Error al cargar la orden</div>;
  
  return (
    <div>
      <h1>Orden #{data?.id}</h1>
      <p>Cliente: {data?.cliente}</p>
      {/* ... */}
    </div>
  );
}
```

## Uso con Formularios (React Hook Form)

```typescript
import { useForm } from 'react-hook-form';
import { NuevoActualizarDireccionRequest } from 'interfaces-mensajeria';

function DireccionForm() {
  const { register, handleSubmit, formState: { errors } } = useForm<NuevoActualizarDireccionRequest>();
  
  const onSubmit = async (data: NuevoActualizarDireccionRequest) => {
    // Enviar datos al servidor
    await axios.post('/api/direccion/guardar', data);
  };
  
  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <input {...register('direccion.alias', { required: true })} placeholder='Alias' />
      {errors.direccion?.alias && <span>Este campo es requerido</span>}
      
      <input {...register('direccion.calle', { required: true })} placeholder='Calle' />
      {errors.direccion?.calle && <span>Este campo es requerido</span>}
      
      {/* ... */}
      
      <button type='submit'>Guardar</button>
    </form>
  );
}
```

## Uso con Redux Toolkit

```typescript
import { createSlice } from '@reduxjs/toolkit';
import { NuevoActualizarDireccionRequest } from 'interfaces-mensajeria';

const initialState = {
  direccion: {
    alias: '',
    calle: '',
    // ...
  },
};

const slice = createSlice({
  name: 'direccion',
  initialState,
  reducers: {
    guardarDireccion(state, action: PayloadAction<NuevoActualizarDireccionRequest>) {
      state.direccion = action.payload;
    },
  },
});

export const { guardarDireccion } = slice.actions;
export default slice.reducer;
```

## Uso con GraphQL

```typescript
import { gql } from '@apollo/client';
import { NuevoActualizarDireccionRequest } from 'interfaces-mensajeria';

const GUARDAR_DIRECCION_MUTATION = gql`
  mutation GuardarDireccion($direccion: NuevoActualizarDireccionRequest!) {
    guardarDireccion(direccion: $direccion) {
      id
      alias
      calle
      // ...
    }
  }
`;

const { data, loading, error } = useMutation(GUARDAR_DIRECCION_MUTATION, {
  variables: {
    direccion: {
      alias: 'Mi dirección',
      calle: 'Calle 123',
      // ...
    },
  },
});
```

";

        await File.WriteAllTextAsync(Path.Combine(outputDir, "docs", "EJEMPLOS.md"), ejemplosMd);
        Console.WriteLine("Generado archivo EJEMPLOS.md");
    }
}
