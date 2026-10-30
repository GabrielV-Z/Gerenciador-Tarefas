# Gerenciador de Tarefas

Aplicativo de console em C# para organizar tarefas pessoais. Cada tarefa tem título,
descrição, prioridade, prazo e etiquetas. Os dados ficam em um arquivo JSON.

## Recursos

- Adicionar, editar, concluir, reabrir e remover tarefas
- Prioridades baixa, média e alta
- Prazos com detecção de tarefas atrasadas
- Etiquetas e busca por texto, prioridade, etiqueta e situação
- Listagem ordenada: pendentes primeiro, atrasadas em seguida, depois por prioridade e prazo
- Estatísticas de andamento
- Persistência em JSON com gravação segura (arquivo temporário seguido de troca)

## Requisitos

SDK do .NET 8 ou mais recente (https://dotnet.microsoft.com/download).
No VS Code, instale também a extensão C# Dev Kit.

## Como executar

Na pasta do projeto:

    dotnet run --project src/GerenciadorTarefas

Os dados ficam em `~/.tarefas/tarefas.json`. Para usar outro arquivo, defina a
variável de ambiente `TAREFAS_ARQUIVO`.

## Testes

    dotnet test tests/GerenciadorTarefas.Tests

## Estrutura

    src/GerenciadorTarefas/
        Modelos/      Tarefa e Prioridade
        Servicos/     regras de negócio e repositório JSON
        Interface/    menu de console
        Program.cs    ponto de entrada
    tests/GerenciadorTarefas.Tests/
        testes de unidade com xUnit

O serviço depende de uma interface de repositório e de um relógio injetável, o que
permite testar as regras (prazos, atrasos, ordenação) sem tocar em disco.
