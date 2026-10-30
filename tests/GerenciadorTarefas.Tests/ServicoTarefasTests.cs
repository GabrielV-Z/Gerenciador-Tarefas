using GerenciadorTarefas.Modelos;
using GerenciadorTarefas.Servicos;

namespace GerenciadorTarefas.Tests;

public class ServicoTarefasTests
{
    private static readonly DateTime Hoje = new(2026, 10, 30, 10, 0, 0);

    private static ServicoTarefas Criar(RepositorioMemoria? repo = null)
    {
        return new ServicoTarefas(repo ?? new RepositorioMemoria(), () => Hoje);
    }

    [Fact]
    public void Adicionar_GeraIdsSequenciais()
    {
        var servico = Criar();

        var a = servico.Adicionar("primeira");
        var b = servico.Adicionar("segunda");

        Assert.Equal(1, a.Id);
        Assert.Equal(2, b.Id);
    }

    [Fact]
    public void Adicionar_RemoveEspacosDoTitulo()
    {
        var tarefa = Criar().Adicionar("  estudar  ");

        Assert.Equal("estudar", tarefa.Titulo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Adicionar_TituloVazio_Lanca(string titulo)
    {
        Assert.Throws<ArgumentException>(() => Criar().Adicionar(titulo));
    }

    [Fact]
    public void Adicionar_TituloMuitoLongo_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar().Adicionar(new string('a', 121)));
    }

    [Fact]
    public void Adicionar_PrazoNoPassado_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar().Adicionar("x", prazo: Hoje.AddDays(-1)));
    }

    [Fact]
    public void Adicionar_PrazoHoje_EhAceito()
    {
        var tarefa = Criar().Adicionar("x", prazo: Hoje);

        Assert.Equal(Hoje.Date, tarefa.Prazo);
    }

    [Fact]
    public void Adicionar_NormalizaEtiquetas()
    {
        var tarefa = Criar().Adicionar("x", etiquetas: new[] { " Casa ", "casa", "", "Trabalho" });

        Assert.Equal(new[] { "casa", "trabalho" }, tarefa.Etiquetas);
    }

    [Fact]
    public void Concluir_MarcaDataEImpedeRepeticao()
    {
        var servico = Criar();
        var tarefa = servico.Adicionar("x");

        servico.Concluir(tarefa.Id);

        Assert.True(tarefa.Concluida);
        Assert.Equal(Hoje, tarefa.ConcluidaEm);
        Assert.Throws<InvalidOperationException>(() => servico.Concluir(tarefa.Id));
    }

    [Fact]
    public void Reabrir_LimpaConclusao()
    {
        var servico = Criar();
        var tarefa = servico.Adicionar("x");
        servico.Concluir(tarefa.Id);

        servico.Reabrir(tarefa.Id);

        Assert.False(tarefa.Concluida);
        Assert.Null(tarefa.ConcluidaEm);
        Assert.Throws<InvalidOperationException>(() => servico.Reabrir(tarefa.Id));
    }

    [Fact]
    public void IdInexistente_Lanca()
    {
        var servico = Criar();

        Assert.Throws<TarefaNaoEncontradaException>(() => servico.Concluir(99));
        Assert.Throws<TarefaNaoEncontradaException>(() => servico.Remover(99));
        Assert.Throws<TarefaNaoEncontradaException>(() => servico.Editar(99, "novo"));
    }

    [Fact]
    public void Remover_TiraDaLista()
    {
        var servico = Criar();
        var tarefa = servico.Adicionar("x");

        servico.Remover(tarefa.Id);

        Assert.Empty(servico.Listar());
    }

    [Fact]
    public void Editar_AlteraApenasOQueFoiInformado()
    {
        var servico = Criar();
        var tarefa = servico.Adicionar("antigo", Prioridade.Baixa, Hoje.AddDays(2));

        servico.Editar(tarefa.Id, titulo: "novo", prioridade: Prioridade.Alta);

        Assert.Equal("novo", tarefa.Titulo);
        Assert.Equal(Prioridade.Alta, tarefa.Prioridade);
        Assert.Equal(Hoje.AddDays(2).Date, tarefa.Prazo);
    }

    [Fact]
    public void Editar_ComDadoInvalido_NaoAlteraNada()
    {
        var servico = Criar();
        var tarefa = servico.Adicionar("original");

        Assert.Throws<ArgumentException>(() => servico.Editar(tarefa.Id, "novo", prazo: Hoje.AddDays(-3)));

        Assert.Equal("original", tarefa.Titulo);
    }

    [Fact]
    public void Listar_OrdenaPorSituacaoAtrasoEPrioridade()
    {
        var repo = new RepositorioMemoria();
        var antigo = new ServicoTarefas(repo, () => Hoje.AddDays(-10));
        antigo.Adicionar("atrasada", Prioridade.Media, Hoje.AddDays(-5));

        var servico = Criar(repo);
        servico.Adicionar("baixa", Prioridade.Baixa);
        servico.Adicionar("alta", Prioridade.Alta);
        var feita = servico.Adicionar("feita", Prioridade.Alta);
        servico.Concluir(feita.Id);

        var titulos = servico.Listar().Select(t => t.Titulo).ToArray();

        Assert.Equal(new[] { "atrasada", "alta", "baixa", "feita" }, titulos);
    }

    [Fact]
    public void Listar_Filtros()
    {
        var servico = Criar();
        servico.Adicionar("comprar pão", Prioridade.Baixa, etiquetas: new[] { "casa" });
        servico.Adicionar("relatório mensal", Prioridade.Alta, descricao: "enviar ao chefe", etiquetas: new[] { "trabalho" });
        var feita = servico.Adicionar("ligar para o banco", Prioridade.Media);
        servico.Concluir(feita.Id);

        Assert.Single(servico.Listar(new FiltroTarefas(Etiqueta: "CASA")));
        Assert.Single(servico.Listar(new FiltroTarefas(Nivel: Prioridade.Alta)));
        Assert.Single(servico.Listar(new FiltroTarefas(Texto: "CHEFE")));
        Assert.Single(servico.Listar(new FiltroTarefas(Concluida: true)));
        Assert.Equal(2, servico.Listar(new FiltroTarefas(Concluida: false)).Count);
    }

    [Fact]
    public void Estatisticas_ContamCorretamente()
    {
        var repo = new RepositorioMemoria();
        new ServicoTarefas(repo, () => Hoje.AddDays(-10)).Adicionar("atrasada", prazo: Hoje.AddDays(-5));
        var servico = Criar(repo);
        servico.Adicionar("a");
        var feita = servico.Adicionar("b");
        servico.Concluir(feita.Id);

        var e = servico.ObterEstatisticas();

        Assert.Equal(3, e.Total);
        Assert.Equal(1, e.Concluidas);
        Assert.Equal(2, e.Pendentes);
        Assert.Equal(1, e.Atrasadas);
        Assert.Equal(100.0 / 3, e.PercentualConcluido, 5);
    }

    [Fact]
    public void Persistencia_NovoServicoEnxergaOsDados()
    {
        var repo = new RepositorioMemoria();
        var primeiro = Criar(repo);
        primeiro.Adicionar("guardada", Prioridade.Alta);

        var segundo = Criar(repo);

        var tarefa = Assert.Single(segundo.Listar());
        Assert.Equal("guardada", tarefa.Titulo);
    }

    [Fact]
    public void RepositorioJson_PreservaDados()
    {
        var pasta = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var caminho = Path.Combine(pasta, "dados", "tarefas.json");
            var repo = new RepositorioJson(caminho);
            Assert.Empty(repo.Carregar());

            var servico = new ServicoTarefas(repo, () => Hoje);
            servico.Adicionar("pagar conta", Prioridade.Alta, Hoje.AddDays(3), "luz", new[] { "casa" });

            var recarregado = new ServicoTarefas(new RepositorioJson(caminho), () => Hoje).Listar();

            var tarefa = Assert.Single(recarregado);
            Assert.Equal("pagar conta", tarefa.Titulo);
            Assert.Equal(Prioridade.Alta, tarefa.Prioridade);
            Assert.Equal(Hoje.AddDays(3).Date, tarefa.Prazo);
            Assert.Equal(new[] { "casa" }, tarefa.Etiquetas);
        }
        finally
        {
            if (Directory.Exists(pasta))
            {
                Directory.Delete(pasta, true);
            }
        }
    }

    [Fact]
    public void RepositorioJson_ArquivoCorrompido_Lanca()
    {
        var arquivo = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(arquivo, "{ isto nao e json valido");

            Assert.Throws<InvalidDataException>(() => new RepositorioJson(arquivo).Carregar());
        }
        finally
        {
            File.Delete(arquivo);
        }
    }
}
