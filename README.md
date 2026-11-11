# ProjetoISP.Baldan

Sistema de Inspeção de Segurança Periódica (ISP) para automação e gestão de conformidade industrial.

O ProjetoISP.Baldan substitui controles manuais e folhas físicas por uma plataforma centralizada desenvolvida em ASP.NET Core MVC com persistência NoSQL em Google Cloud Firestore.

## Layout do repositório

```text
Controllers/              Controladores de rotas e lógica de aplicação MVC
Models/                   Entidades de domínio adaptadas ao Cloud Firestore
Views/                    Interfaces de utilizador em Razor Pages
wwwroot/                  Recursos estáticos (CSS, JS, imagens)
Program.cs                Configuração de DI e credenciais do Firebase
appsettings.json          Configurações gerais do ambiente
```

## Pré-requisitos

-.NET 8 SDK
-Visual Studio 2022 (com workload ASP.NET e desenvolvimento Web)
-Ficheiro de credenciais firebase-auth.json configurado

## Iniciar o ambiente de desenvolvimento

Clonar o repositório

```bash
git clone [https://github.com/YgorSilva-Git/projetoispbaldan.git](https://github.com/YgorSilva-Git/projetoispbaldan.git)
```

Adiciona o ficheiro de chave de serviço firebase-auth.json na raiz do projeto ProjetoISP.Baldan/.

Exeuta a aplicação localmente:

```bash
dotnet restore
dotnet run --project ProjetoISP.Baldan
```
Abre ```http://localhost:5000``` (ou a porta HTTPS indicada) para aceder à aplicação.

## Verificações de qualidade
Para validar o código e garantir que o projeto compila sem erros:

```bash
dotnet build
```

## Primeiro marco (MVP)
O fluxo principal estabelece a seguinte pipeline de dados:

Plaintext
Interface (Razor Views) -> Controllers -> SDK Firestore -> Google Cloud Firestore

## Documentação e Parceria
Desenvolvido no âmbito do curso de Desenvolvimento de Software Multiplataforma da Fatec Matão em colaboração com a Baldan.
