using SmartSkills.Core.Scanning;
using Xunit;

namespace SmartSkills.Core.Tests;

public class MavenPomPackageResolverTests
{
    [Fact]
    public void ParsePom_StandardDependencies_ReturnsAll()
    {
        var pomXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0">
                <dependencies>
                    <dependency>
                        <groupId>com.azure</groupId>
                        <artifactId>azure-identity</artifactId>
                        <version>1.15.0</version>
                    </dependency>
                    <dependency>
                        <groupId>com.azure</groupId>
                        <artifactId>azure-cosmos</artifactId>
                        <version>4.50.0</version>
                    </dependency>
                </dependencies>
            </project>
            """;

        var packages = MavenPomPackageResolver.ParsePom(pomXml);

        Assert.Equal(2, packages.Count);
        Assert.All(packages, p => Assert.Equal(Ecosystems.Java, p.Ecosystem));
        Assert.Contains(packages, p => p.Name == "com.azure:azure-identity" && p.Version == "1.15.0");
        Assert.Contains(packages, p => p.Name == "com.azure:azure-cosmos" && p.Version == "4.50.0");
    }

    [Fact]
    public void ParsePom_PropertyResolution_ResolvesVersions()
    {
        var pomXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0">
                <properties>
                    <azure.sdk.version>1.15.0</azure.sdk.version>
                </properties>
                <dependencies>
                    <dependency>
                        <groupId>com.azure</groupId>
                        <artifactId>azure-identity</artifactId>
                        <version>${azure.sdk.version}</version>
                    </dependency>
                </dependencies>
            </project>
            """;

        var packages = MavenPomPackageResolver.ParsePom(pomXml);

        Assert.Single(packages);
        Assert.Equal("1.15.0", packages[0].Version);
    }

    [Fact]
    public void ParsePom_BomImport_Skipped()
    {
        var pomXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0">
                <dependencies>
                    <dependency>
                        <groupId>com.azure</groupId>
                        <artifactId>azure-sdk-bom</artifactId>
                        <version>1.2.25</version>
                        <type>pom</type>
                        <scope>import</scope>
                    </dependency>
                    <dependency>
                        <groupId>com.azure</groupId>
                        <artifactId>azure-storage-blob</artifactId>
                    </dependency>
                </dependencies>
            </project>
            """;

        var packages = MavenPomPackageResolver.ParsePom(pomXml);

        Assert.Single(packages);
        Assert.Equal("com.azure:azure-storage-blob", packages[0].Name);
    }

    [Fact]
    public void ParsePom_TestScopeDependency_StillIncluded()
    {
        var pomXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0">
                <dependencies>
                    <dependency>
                        <groupId>org.junit.jupiter</groupId>
                        <artifactId>junit-jupiter</artifactId>
                        <version>5.10.0</version>
                        <scope>test</scope>
                    </dependency>
                </dependencies>
            </project>
            """;

        var packages = MavenPomPackageResolver.ParsePom(pomXml);

        Assert.Single(packages);
        Assert.Equal("org.junit.jupiter:junit-jupiter", packages[0].Name);
    }

    [Fact]
    public void ParsePom_NoNamespace_StillParses()
    {
        var pomXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project>
                <dependencies>
                    <dependency>
                        <groupId>com.azure</groupId>
                        <artifactId>azure-identity</artifactId>
                        <version>1.0.0</version>
                    </dependency>
                </dependencies>
            </project>
            """;

        var packages = MavenPomPackageResolver.ParsePom(pomXml);

        Assert.Single(packages);
        Assert.Equal("com.azure:azure-identity", packages[0].Name);
    }

    [Fact]
    public void ParsePom_EmptyDependencies_ReturnsEmpty()
    {
        var pomXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0">
                <dependencies />
            </project>
            """;

        var packages = MavenPomPackageResolver.ParsePom(pomXml);

        Assert.Empty(packages);
    }

    [Fact]
    public void ParsePom_NoDependenciesSection_ReturnsEmpty()
    {
        var pomXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0">
                <modelVersion>4.0.0</modelVersion>
            </project>
            """;

        var packages = MavenPomPackageResolver.ParsePom(pomXml);

        Assert.Empty(packages);
    }

    [Fact]
    public void ParsePom_MissingVersionAttribute_ReturnsEmptyVersion()
    {
        var pomXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0">
                <dependencies>
                    <dependency>
                        <groupId>com.azure</groupId>
                        <artifactId>azure-storage-blob</artifactId>
                    </dependency>
                </dependencies>
            </project>
            """;

        var packages = MavenPomPackageResolver.ParsePom(pomXml);

        Assert.Single(packages);
        Assert.Equal("", packages[0].Version);
    }

    [Fact]
    public void ParsePom_DependencyManagementIgnored()
    {
        var pomXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0">
                <dependencyManagement>
                    <dependencies>
                        <dependency>
                            <groupId>com.azure</groupId>
                            <artifactId>azure-sdk-bom</artifactId>
                            <version>1.2.25</version>
                            <type>pom</type>
                            <scope>import</scope>
                        </dependency>
                    </dependencies>
                </dependencyManagement>
                <dependencies>
                    <dependency>
                        <groupId>com.azure</groupId>
                        <artifactId>azure-cosmos</artifactId>
                        <version>4.50.0</version>
                    </dependency>
                </dependencies>
            </project>
            """;

        var packages = MavenPomPackageResolver.ParsePom(pomXml);

        Assert.Single(packages);
        Assert.Equal("com.azure:azure-cosmos", packages[0].Name);
    }

    [Fact]
    public void ParsePom_FromFile_ReturnsExpectedPackages()
    {
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Java", "pom.xml");
        var pomXml = File.ReadAllText(testDataPath);

        var packages = MavenPomPackageResolver.ParsePom(pomXml);

        // Expect: azure-identity, azure-cosmos, azure-storage-blob, junit-jupiter
        // (BOM import should be skipped)
        Assert.Equal(4, packages.Count);
        Assert.Contains(packages, p => p.Name == "com.azure:azure-identity" && p.Version == "1.15.0");
        Assert.Contains(packages, p => p.Name == "com.azure:azure-cosmos" && p.Version == "4.50.0");
        Assert.Contains(packages, p => p.Name == "com.azure:azure-storage-blob" && p.Version == "");
        Assert.Contains(packages, p => p.Name == "org.junit.jupiter:junit-jupiter" && p.Version == "5.10.0");
    }
}
