FROM maven:3.9-eclipse-temurin-25 AS build
WORKDIR /app

COPY pom.xml mvnw ./
COPY .mvn .mvn
RUN ./mvnw dependency:go-offline

COPY src src
RUN ./mvnw clean package -DskipTests

FROM eclipse-temurin:25-jre
WORKDIR /app

RUN groupadd --gid 10001 spring \
    && useradd --uid 10001 --gid spring --no-create-home --shell /usr/sbin/nologin spring

COPY --from=build --chown=spring:spring /app/target/*.jar app.jar

EXPOSE 8080

USER spring

ENTRYPOINT ["java", "-jar", "app.jar"]
