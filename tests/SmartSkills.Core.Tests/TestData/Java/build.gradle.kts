plugins {
    kotlin("jvm") version "1.9.21"
    id("org.springframework.boot") version "3.2.0"
}

repositories {
    mavenCentral()
}

dependencies {
    implementation("com.azure:azure-identity:1.15.0")
    implementation("com.azure:azure-cosmos:4.50.0")
    api("com.azure:azure-storage-blob:12.25.0")
    testImplementation("org.junit.jupiter:junit-jupiter:5.10.0")
    implementation("com.azure:azure-data-tables")
}
