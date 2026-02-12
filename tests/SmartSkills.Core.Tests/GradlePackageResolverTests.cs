using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class GradlePackageResolverTests
{
    [Fact]
    public void ParseBuildFile_GroovyDsl_ReturnsAll()
    {
        var groovy = """
            dependencies {
                implementation 'com.azure:azure-identity:1.15.0'
                implementation 'com.azure:azure-cosmos:4.50.0'
                testImplementation 'org.junit.jupiter:junit-jupiter:5.10.0'
            }
            """;

        var packages = GradlePackageResolver.ParseBuildFile(groovy);

        Assert.Equal(3, packages.Count);
        Assert.All(packages, p => Assert.Equal(Ecosystems.Java, p.Ecosystem));
        Assert.Contains(packages, p => p.Name == "com.azure:azure-identity" && p.Version == "1.15.0");
        Assert.Contains(packages, p => p.Name == "com.azure:azure-cosmos" && p.Version == "4.50.0");
    }

    [Fact]
    public void ParseBuildFile_KotlinDsl_ReturnsAll()
    {
        var kotlin = """
            dependencies {
                implementation("com.azure:azure-identity:1.15.0")
                implementation("com.azure:azure-cosmos:4.50.0")
                testImplementation("org.junit.jupiter:junit-jupiter:5.10.0")
            }
            """;

        var packages = GradlePackageResolver.ParseBuildFile(kotlin);

        Assert.Equal(3, packages.Count);
        Assert.Contains(packages, p => p.Name == "com.azure:azure-identity");
        Assert.Contains(packages, p => p.Name == "com.azure:azure-cosmos");
    }

    [Fact]
    public void ParseBuildFile_AllKnownConfigurations_Accepted()
    {
        var buildFile = """
            dependencies {
                implementation 'g:a1:1.0'
                api 'g:a2:1.0'
                compileOnly 'g:a3:1.0'
                runtimeOnly 'g:a4:1.0'
                testImplementation 'g:a5:1.0'
                testCompileOnly 'g:a6:1.0'
                testRuntimeOnly 'g:a7:1.0'
                annotationProcessor 'g:a8:1.0'
                testAnnotationProcessor 'g:a9:1.0'
                compileOnlyApi 'g:a10:1.0'
                kapt 'g:a11:1.0'
            }
            """;

        var packages = GradlePackageResolver.ParseBuildFile(buildFile);

        Assert.Equal(11, packages.Count);
    }

    [Fact]
    public void ParseBuildFile_UnknownConfiguration_Ignored()
    {
        var buildFile = """
            dependencies {
                classpath 'com.android.tools.build:gradle:8.1.0'
                implementation 'com.azure:azure-identity:1.0.0'
            }
            """;

        var packages = GradlePackageResolver.ParseBuildFile(buildFile);

        Assert.Single(packages);
        Assert.Equal("com.azure:azure-identity", packages[0].Name);
    }

    [Fact]
    public void ParseBuildFile_NoVersion_ReturnsEmptyVersion()
    {
        var buildFile = """
            dependencies {
                implementation("com.azure:azure-data-tables")
            }
            """;

        var packages = GradlePackageResolver.ParseBuildFile(buildFile);

        Assert.Single(packages);
        Assert.Equal("com.azure:azure-data-tables", packages[0].Name);
        Assert.Equal("", packages[0].Version);
    }

    [Fact]
    public void ParseBuildFile_DuplicateDependency_Deduplicated()
    {
        var buildFile = """
            dependencies {
                implementation 'com.azure:azure-identity:1.15.0'
                testImplementation 'com.azure:azure-identity:1.15.0'
            }
            """;

        var packages = GradlePackageResolver.ParseBuildFile(buildFile);

        Assert.Single(packages);
    }

    [Fact]
    public void ParseBuildFile_EmptyDependencies_ReturnsEmpty()
    {
        var buildFile = """
            dependencies {
            }
            """;

        var packages = GradlePackageResolver.ParseBuildFile(buildFile);

        Assert.Empty(packages);
    }

    [Fact]
    public void ParseBuildFile_FromGroovyFile_ReturnsExpected()
    {
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Java", "build.gradle");
        var buildFile = File.ReadAllText(testDataPath);

        var packages = GradlePackageResolver.ParseBuildFile(buildFile);

        // azure-identity, azure-cosmos, azure-storage-blob, lombok (once, deduplicated), mssql-jdbc, junit-jupiter
        Assert.Equal(6, packages.Count);
        Assert.Contains(packages, p => p.Name == "com.azure:azure-identity");
        Assert.Contains(packages, p => p.Name == "com.azure:azure-cosmos");
        Assert.Contains(packages, p => p.Name == "com.azure:azure-storage-blob");
    }

    [Fact]
    public void ParseBuildFile_FromKotlinFile_ReturnsExpected()
    {
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Java", "build.gradle.kts");
        var buildFile = File.ReadAllText(testDataPath);

        var packages = GradlePackageResolver.ParseBuildFile(buildFile);

        Assert.Equal(5, packages.Count);
        Assert.Contains(packages, p => p.Name == "com.azure:azure-identity");
        Assert.Contains(packages, p => p.Name == "com.azure:azure-data-tables" && p.Version == "");
    }
}
