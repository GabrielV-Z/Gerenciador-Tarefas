using System.Globalization;
using GerenciadorTarefas.Modelos;
using GerenciadorTarefas.Servicos;

namespace GerenciadorTarefas.Interface;

public class MenuConsole
{
    private const string FormatoData = "dd/MM/yyyy";

    private readonly ServicoTarefas _servico;

    public MenuConsole(ServicoTarefas servico)
    {
        _servico = servico;
    }

    public void Executar()
    {
        while (true)
        {
            MostrarMenu();
            var opcao = Console.ReadLine()?.Trim();
            if (opcao is null || opcao == "0")
            {
                break;
            }

            try
            {
                Despachar(opcao);
            }
            catch (Exception ex) when (ex is ArgumentException
                                          or TarefaNaoEncontradaException
                                          or InvalidOperationException)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }

            Console.WriteLine();
        }
    }

    private static void MostrarMenu()
    {
        Console.WriteLine("=== Gerenciador de Tarefas ===");
        Console.WriteLine("1  Adicionar tarefa");
        Console.WriteLine("2  Listar todas");
        Console.WriteLine("3  Buscar e filtrar");
        Console.WriteLine("4  Concluir tarefa");
        Console.WriteLine("5  Reabrir tarefa");
        Console.WriteLine("6  Editar tarefa");
        Console.WriteLine("7  Remover tarefa");
        Console.WriteLine("8  Estatísticas");
        Console.WriteLine("0  Sair");
        Console.Write("Opção: ");
    }

    private void Despachar(string opcao)
    {
        switch (opcao)
        {
            case "1": Adicionar(); break;
            case "2": Listar(new FiltroTarefas()); break;
            case "3": Filtrar(); break;
            case "4": Concluir(); break;
            case "5": Reabrir(); break;
            case "6": Editar(); break;
            case "7": Remover(); break;
            case "8": MostrarEstatisticas(); break;
            default: Console.WriteLine("Opção inválida."); break;
        }
    }

    private void Adicionar()
    {
        var titulo = Ler("Título: ");
        var descricao = Ler("Descrição (opcional): ");
        var prioridade = LerPrioridade("Prioridade [baixa/média/alta] (Enter = média): ") ?? Prioridade.Media;
        var prazo = LerData("Prazo dd/MM/aaaa (Enter = sem prazo): ");
        var etiquetas = Ler("Etiquetas separadas por vírgula (opcional): ")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var tarefa = _servico.Adicionar(titulo, prioridade, prazo, descricao, etiquetas);
        Console.WriteLine($"Tarefa #{tarefa.Id} criada.");
    }

    private void Filtrar()
    {
        var texto = Ler("Texto no título ou descrição (Enter = ignorar): ");
        var etiqueta = Ler("Etiqueta (Enter = ignorar): ");
        var nivel = LerPrioridade("Prioridade (Enter = ignorar): ");
        var situacao = Ler("Situação [pendentes/concluídas/atrasadas] (Enter = todas): ").ToLowerInvariant();

        bool? concluida = situacao switch
        {
            "pendentes" => false,
            "concluidas" or "concluídas" => true,
            _ => null
        };

        var filtro = new FiltroTarefas(
            Concluida: concluida,
            Nivel: nivel,
            Etiqueta: etiqueta.Length > 0 ? etiqueta : null,
            ApenasAtrasadas: situacao == "atrasadas",
            Texto: texto.Length > 0 ? texto : null);

        Listar(filtro);
    }

    private void Listar(FiltroTarefas filtro)
    {
        var tarefas = _servico.Listar(filtro);
        if (tarefas.Count == 0)
        {
            Console.WriteLine("Nenhuma tarefa encontrada.");
            return;
        }

        var linhas = tarefas
            .Select(t => new[]
            {
                t.Id.ToString(CultureInfo.InvariantCulture),
                t.Concluida ? "[x]" : "[ ]",
                t.Prioridade.ToString(),
                t.Prazo?.ToString(FormatoData, CultureInfo.InvariantCulture) ?? "-",
                t.Titulo + (_servico.EstaAtrasada(t) ? "  (ATRASADA)" : string.Empty),
                string.Join(", ", t.Etiquetas)
            })
            .ToList();

        Tabela(new[] { "ID", "", "Prioridade", "Prazo", "Título", "Etiquetas" }, linhas);
    }

    private void Concluir()
    {
        var tarefa = _servico.Concluir(LerId("Id da tarefa: "));
        Console.WriteLine($"Tarefa #{tarefa.Id} concluída.");
    }

    private void Reabrir()
    {
        var tarefa = _servico.Reabrir(LerId("Id da tarefa: "));
        Console.WriteLine($"Tarefa #{tarefa.Id} reaberta.");
    }

    private void Editar()
    {
        var id = LerId("Id da tarefa: ");
        var titulo = Ler("Novo título (Enter = manter): ");
        var prioridade = LerPrioridade("Nova prioridade (Enter = manter): ");
        var prazo = LerData("Novo prazo dd/MM/aaaa (Enter = manter): ");

        var tarefa = _servico.Editar(id, titulo.Length > 0 ? titulo : null, prioridade, prazo);
        Console.WriteLine($"Tarefa #{tarefa.Id} atualizada.");
    }

    private void Remover()
    {
        var id = LerId("Id da tarefa: ");
        var confirmacao = Ler("Confirma a remoção? (s/N): ").ToLowerInvariant();
        if (confirmacao != "s")
        {
            Console.WriteLine("Remoção cancelada.");
            return;
        }

        _servico.Remover(id);
        Console.WriteLine($"Tarefa #{id} removida.");
    }

    private void MostrarEstatisticas()
    {
        var e = _servico.ObterEstatisticas();
        Console.WriteLine($"Total:      {e.Total}");
        Console.WriteLine($"Concluídas: {e.Concluidas} ({e.PercentualConcluido:0.#}%)");
        Console.WriteLine($"Pendentes:  {e.Pendentes}");
        Console.WriteLine($"Atrasadas:  {e.Atrasadas}");
    }

    private static string Ler(string pergunta)
    {
        Console.Write(pergunta);
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static int LerId(string pergunta)
    {
        var texto = Ler(pergunta);
        if (!int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            throw new ArgumentException("Informe um número de id válido.");
        }

        return id;
    }

    private static Prioridade? LerPrioridade(string pergunta)
    {
        var texto = Ler(pergunta).ToLowerInvariant();
        return texto switch
        {
            "" => null,
            "1" or "baixa" => Prioridade.Baixa,
            "2" or "media" or "média" => Prioridade.Media,
            "3" or "alta" => Prioridade.Alta,
            _ => throw new ArgumentException("Prioridade inválida. Use baixa, média ou alta.")
        };
    }

    private static DateTime? LerData(string pergunta)
    {
        var texto = Ler(pergunta);
        if (texto.Length == 0)
        {
            return null;
        }

        if (!DateTime.TryParseExact(texto, FormatoData, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
        {
            throw new ArgumentException("Data inválida. Use o formato dd/MM/aaaa.");
        }

        return data;
    }

    private static void Tabela(string[] cabecalho, List<string[]> linhas)
    {
        var larguras = cabecalho.Select(c => c.Length).ToArray();
        foreach (var linha in linhas)
        {
            for (var i = 0; i < linha.Length; i++)
            {
                larguras[i] = Math.Max(larguras[i], linha[i].Length);
            }
        }

        string Formatar(string[] celulas) =>
            string.Join("  ", celulas.Select((c, i) => c.PadRight(larguras[i]))).TrimEnd();

        Console.WriteLine(Formatar(cabecalho));
        Console.WriteLine(string.Join("  ", larguras.Select(l => new string('-', l))));
        foreach (var linha in linhas)
        {
            Console.WriteLine(Formatar(linha));
        }
    }
}
