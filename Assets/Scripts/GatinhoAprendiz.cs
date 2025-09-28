using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GatinhoAprendiz : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds2 = new(2f);
    #region Singleton
    public static GatinhoAprendiz Instance;

    void Awake()
    {
        Instance = this;
    }
    #endregion

    #region UI References
    public TMP_Text pensamento;
    public GameObject balaoPensamento;
    public Animator animator;
    public PopUpController popUpController;
    #endregion

    #region Movement and Exploration
    public Transform pontoAnalise;
    public Transform[] pontosExploracao;
    public float velocidade = 2f;
    private int indexExploracao = 0;
    private bool explorando = false;
    private List<Transform> pontosExploracaoDinamicos = new();
    private bool emTreinamentoExtra = false;
    private bool explicacaoExtraMostrada = false;
    private bool explicacaoBoostingMostrada = false;
    private bool mostrouAmostragem = false;
    private bool concluiu = false;
    readonly HashSet<GameObject> objetosExplorados = new();

    #endregion

    #region Game State
    private GameObject objetoAtual;
    private List<Color> brinquedosAprendidos = new();
    private HashSet<string> nomesNaoBrinquedos = new();
    private HashSet<string> objetosNegativos = new();
    private int brinquedosMostrados = 0;
    private int acertos = 0;
    private HashSet<Color> coresAprendidasUnicas = new();
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        GerarPontosNosBrinquedos();

        if (popUpController == null)
            popUpController = FindFirstObjectByType<PopUpController>(FindObjectsInactive.Include);

        if (popUpController != null)
        {
            popUpController.gameObject.SetActive(true);

            popUpController.Mostrar(
                "Bem-vindo ao Gatinho Classificador!<br>",
                "Você deve mostrar 2 brinquedos de <b>cores diferentes</b> ao gatinho para que<br>ele aprenda a identificá-los.",
                "Continuar",
                () =>
                {
                    popUpController.Mostrar(
                        "",
                        "Depois disso, ele tentará identificar sozinho os brinquedos. "
                            + "Você dirá se ele acertou ou errou, ajudando-o a aprender!<br>"
                            + "Arraste e solte o brinquedo na frente do gatinho.",
                        "Começar!",
                        () =>
                        {
                            popUpController.Fechar();
                        }
                    );
                }
            );
        }
        else
        {
            Debug.LogError("PopUpController não foi encontrado na cena!");
        }
    }

    void Update()
    {
        MovimentarGatinho();
        AtualizarPosicaoBalaoPensamento();

        if (animator != null)
            animator.SetBool("explorando", explorando);

        if (!mostrouAmostragem && PorcentagemAprendizado() >= 0.4f)
        {
            explorando = false;
            objetoAtual = null;
            mostrouAmostragem = true;
            MostrarPopUpAmostragem();
        }

        if (!concluiu && PorcentagemAprendizado() >= 0.7f)
        {
            explorando = false;
            objetoAtual = null;
            concluiu = true;
            MostrarPopUpConclusao();
        }
    }
    #endregion

    #region UI Control
    public void JogadorValidou(bool acertou)
    {
        if (balaoPensamento != null)
            balaoPensamento.SetActive(true);

        if (pensamento != null)
            pensamento.text = acertou ? "Obaa! Acertei!" : "Oh não... errei.";

        if (!acertou && objetoAtual != null)
        {
            string nome = objetoAtual.name;
            if (!nomesNaoBrinquedos.Contains(nome))
            {
                nomesNaoBrinquedos.Add(nome);
                emTreinamentoExtra = true;
                IniciarTreinamentoExtra(objetoAtual);
            }
        }

        if (!emTreinamentoExtra)
            StartCoroutine(EsconderBalaoEContinuar());
    }

    IEnumerator EsconderBalaoEContinuar()
    {
        yield return _waitForSeconds2;

        if (balaoPensamento != null)
            balaoPensamento.SetActive(false);

        if (pontosExploracaoDinamicos.Count > 0)
            indexExploracao = (indexExploracao + 1) % pontosExploracaoDinamicos.Count;

        objetoAtual = null;

        explorando = true;
    }
    #endregion

    #region Movimentação
    private void MovimentarGatinho()
    {
        if (!explorando || pontosExploracaoDinamicos.Count == 0)
            return;

        Transform alvoExploracao = pontosExploracaoDinamicos[indexExploracao];
        if (alvoExploracao == null)
            return;

        GameObject maisProximo = ProcurarObjetoProximo(alvoExploracao.position);

        Vector3 destino =
            maisProximo != null ? maisProximo.transform.position : alvoExploracao.position;

        transform.position = Vector2.MoveTowards(
            transform.position,
            destino,
            velocidade * Time.deltaTime
        );

        VirarParaObjeto(maisProximo != null ? maisProximo : alvoExploracao.gameObject);

        if (Vector2.Distance(transform.position, destino) < 0.1f)
        {
            if (maisProximo != null && !objetosExplorados.Contains(maisProximo))
            {
                objetosExplorados.Add(maisProximo);
                StartCoroutine(VerificarObjeto(maisProximo));
                explorando = false;
                return;
            }

            if (pontosExploracaoDinamicos.Count > 0)
                indexExploracao = (indexExploracao + 1) % pontosExploracaoDinamicos.Count;
        }
    }

    private GameObject ProcurarObjetoProximo(Vector3 centro)
    {
        Collider2D[] col = Physics2D.OverlapCircleAll(centro, 1.0f); 
        GameObject maisProximo = null;
        float menorDistancia = Mathf.Infinity;

        foreach (var c in col)
        {
            if (
                c != null
                && c.CompareTag("ObjetoAnalise")
                && !objetosExplorados.Contains(c.gameObject)
            )
            {
                float dist = Vector2.Distance(transform.position, c.transform.position);
                if (dist < menorDistancia)
                {
                    menorDistancia = dist;
                    maisProximo = c.gameObject;
                }
            }
        }
        return maisProximo;
    }

    void VirarParaObjeto(GameObject obj)
    {
        if (obj == null)
            return;

        float direcaoX = obj.transform.position.x - transform.position.x;
        if (direcaoX > 0.01f)
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        else if (direcaoX < -0.01f)
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
    }

    private void AtualizarPosicaoBalaoPensamento()
    {
        if (balaoPensamento != null && balaoPensamento.activeSelf && Camera.main != null)
        {
            Vector3 posTela = Camera.main.WorldToScreenPoint(
                transform.position + new Vector3(0, 2f, 0)
            );
            balaoPensamento.transform.position = posTela;
        }
    }
    #endregion

    #region Exploração e Análise
    public float PorcentagemAprendizado()
    {
        int totalEsperado = GameObject.FindGameObjectsWithTag("ObjetoAnalise").Length;
        if (totalEsperado == 0)
            return 0f;

        return (float)objetosExplorados.Count / totalEsperado;
    }

    void GerarPontosNosBrinquedos()
    {
        pontosExploracaoDinamicos.Clear();

        GameObject[] brinquedos = GameObject.FindGameObjectsWithTag("ObjetoAnalise");
        foreach (GameObject brinquedo in brinquedos)
        {
            if (brinquedo == null)
                continue;

            Vector3 pos = brinquedo.transform.position;
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            Vector3 pontoFinal = pos + (Vector3)offset;

            GameObject ponto = new("PontoExploracao_" + brinquedo.name);
            ponto.transform.position = pontoFinal;
            pontosExploracaoDinamicos.Add(ponto.transform);
        }

        pontosExploracao = pontosExploracaoDinamicos.ToArray();
    }

    public void TentarAnalisarObjeto(GameObject obj)
    {
        if (obj == null)
            return;

        VirarParaObjeto(obj);
        float distancia = Vector2.Distance(transform.position, obj.transform.position);
        if (distancia < 2f)
        {
            objetoAtual = obj;
            StartCoroutine(FazerAnalise(obj));
        }
    }

    #endregion

    #region Verificação
    IEnumerator VerificarObjeto(GameObject obj)
    {
        if (obj == null)
            yield break;

        objetoAtual = obj;

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        string nomeCor = "cor indefinida";
        Color cor = Color.clear;
        bool achouCor = false;
        string chaveNomeCor = null;

        if (sr != null)
        {
            cor = sr.color;
            nomeCor = NomeDaCor(cor);
            achouCor = brinquedosAprendidos.Any(c => CoresSemelhantes(c, cor));
            chaveNomeCor = obj.name + "_" + nomeCor;
        }

        bool negativeByName =
            nomesNaoBrinquedos.Contains(obj.name) || objetosNegativos.Contains(chaveNomeCor);

        string prediction;
        if (achouCor && !negativeByName)
            prediction = "toy";
        else if (!achouCor)
            prediction = "unknown";
        else
            prediction = "nottoy";

        if (balaoPensamento != null)
            balaoPensamento.SetActive(true);

        if (prediction == "toy")
        {
            if (pensamento != null)
                pensamento.text = $"Já vi brinquedos dessa cor ({nomeCor})...";
            yield return _waitForSeconds2;
            if (pensamento != null)
                pensamento.text = $"Acredito que esse {obj.name} seja um brinquedo!";
        }
        else if (prediction == "unknown")
        {
            if (pensamento != null)
                pensamento.text = $"Hmm... não conheço essa cor ({nomeCor}) no {obj.name}.";
            yield return _waitForSeconds2;
            if (pensamento != null)
                pensamento.text = $"Acho que não é brinquedo!";
        }
        else // "nottoy"
        {
            if (pensamento != null)
                pensamento.text =
                    $"Já vi algo parecido, mas o nome {obj.name} está marcado como <b>não</b> brinquedo.";
            yield return _waitForSeconds2;
            if (pensamento != null)
                pensamento.text = $"Acredito que não seja um brinquedo.";
        }

        yield return _waitForSeconds2;

        if (balaoPensamento != null)
            balaoPensamento.SetActive(false);

        if (popUpController != null)
        {
            GameObject objetoCapturado = obj;

            if (prediction == "toy")
            {
                popUpController.Mostrar(
                    "Validação",
                    $"O gatinho acha que {obj.name} é um brinquedo. Ele acertou?",
                    "Sim",
                    () =>
                    {
                        JogadorValidou(true);
                        acertos++;
                        if (acertos >= 3 && !explicacaoBoostingMostrada)
                        {
                            explorando = false;
                            objetoAtual = null;
                            StartCoroutine(MostrarBoostingDepoisDoBalao());
                        }
                    },
                    "Não",
                    () =>
                    {
                        JogadorValidou(false);
                    }
                );
            }
            else if (prediction == "unknown")
            {
                popUpController.Mostrar(
                    "Validação",
                    $"O gatinho não conhece a cor ({nomeCor}) do {obj.name}. Esse objeto é um brinquedo?",
                    "Sim",
                    () =>
                    {
                        JogadorValidou(true);
                        AprenderNovaCor(nomeCor, true, obj.name);
                    },
                    "Não",
                    () =>
                    {
                        JogadorValidou(true);
                        AprenderNovaCor(nomeCor, false, obj.name);
                    }
                );
            }
            else // "nottoy"
            {
                popUpController.Mostrar(
                    "Validação",
                    $"O gatinho acha que {obj.name} <b>não</b> é um brinquedo. Ele acertou?",
                    "Sim",
                    () =>
                    {
                        JogadorValidou(true);
                    },
                    "Não",
                    () =>
                    {
                        JogadorValidou(false);
                    }
                );
            }
        }
        else
        {
            JogadorValidou(false);
        }
    }

    private IEnumerator MostrarBoostingDepoisDoBalao()
    {
        yield return EsconderBalaoEContinuar();

        MostrarPopUpBoosting();
    }

    private void MostrarPopUpBoosting()
    {
        if (popUpController == null)
            return;

        explorando = false;

        popUpController.Mostrar(
            "Conceito: Boosting",
            "Boosting é o jeito do gatinho aprender aos poucos: cada experiência (acerto ou erro) "
                + "ajuda a ajustar sua memória para melhorar da próxima vez. Ele vai ficando melhor com mais exemplos!",
            "Entendi",
            () =>
            {
                explorando = true;
                explicacaoBoostingMostrada = true;
            }
        );
    }

    #endregion

    #region Treinamento

    IEnumerator FazerAnalise(GameObject obj)
    {
        if (balaoPensamento != null)
            balaoPensamento.SetActive(true);
        if (pensamento != null)
            pensamento.text = "Deixe-me observar esse objeto...";
        yield return _waitForSeconds2;

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        string nomeCor = "cor indefinida";
        Color corObjeto = Color.clear;

        if (sr != null)
        {
            corObjeto = sr.color;
            nomeCor = NomeDaCor(corObjeto);
            brinquedosAprendidos.Add(corObjeto);
            coresAprendidasUnicas.Add(corObjeto);
        }
        brinquedosMostrados++;

        if (pensamento != null)
            pensamento.text =
                $"Entendi! Vou lembrar que o {obj.name} é um brinquedo de cor {nomeCor}.";
        yield return _waitForSeconds2;

        obj.GetComponent<DragDrop>().VoltarPosicaoInicial();
        if (balaoPensamento != null)
            balaoPensamento.SetActive(false);

        if (brinquedosAprendidos.Count == 1 && popUpController != null)
        {
            popUpController.Mostrar(
                "Fase de Treinamento",
                "O gatinho acabou de guardar sua primeira cor.<br>"
                    + "Ele está aprendendo de forma <b>supervisionada</b>.<br>"
                    + "Sempre que você mostrar um brinquedo, ele grava a cor "
                    + "para usar mais tarde e tentar reconhecer sozinho.<br>Mostre um brinquedo de outra cor para o gatinho",
                "Continuar",
                () =>
                {
                    popUpController.Fechar();
                }
            );
        }

        if (brinquedosAprendidos.Count == 2 && popUpController != null)
        {
            popUpController.Mostrar(
                "Fase de Treinamento",
                "Perfeito! Agora o gatinho já conhece cores diferentes<br>"
                    + "Quanto mais exemplos variados ele recebe, "
                    + "mais fácil será para ele criar regras para identificar brinquedos sozinho.<br>"
                    + "Esse é o princípio dos algoritmos de classificação!",
                "Próximo",
                () =>
                {
                    popUpController.Fechar();
                }
            );
        }

        if (brinquedosAprendidos.Count == 2)
            popUpController.Mostrar(
                "Fase de Exploração",
                "Agora é a vez do gatinho explorar!<br>"
                    + "Ele vai usar as pistas que aprendeu (cores) para decidir "
                    + "se o objeto é brinquedo ou não. <br>"
                    + "Isso é como uma <b>árvore de decisão</b> simples "
                    + "onde ele segue 'se a cor é conhecida, então é brinquedo'",
                "Explorar",
                () =>
                {
                    popUpController.Fechar();
                    explorando = true;
                }
            );
    }
    #endregion

    #region Treinamento Extra
    private void IniciarTreinamentoExtra(GameObject obj)
    {
        if (obj == null)
            return;

        explorando = false;

        if (balaoPensamento != null)
            balaoPensamento.SetActive(false);

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        string nomeCor = "cor indefinida";
        Color cor = Color.clear;

        if (sr != null)
        {
            cor = sr.color;
            nomeCor = NomeDaCor(cor);
        }

        if (!explicacaoExtraMostrada)
        {
            popUpController.Mostrar(
                "Treinamento Extra",
                "Ops! O gatinho errou.<br>"
                    + "Agora ele vai aprender que este objeto <b>não</b> é um brinquedo.<br>"
                    + "No aprendizado de máquina, isso é chamado de <b>exemplo negativo</b>.<br>"
                    + "A partir de agora, ele vai comparar não só a cor, mas também o <b>nome</b> do objeto para evitar novos erros.",
                "Continuar",
                () =>
                {
                    popUpController.Mostrar(
                        "Como o gatinho aprende com erros",
                        "Cada erro vira uma <b>pista</b> que ajuda a ajustar sua memória.<br>"
                            + "É como se ele criasse uma <b>regrinha extra</b> só para não repetir esse engano.<br>"
                            + "No CatBoost, isso acontece várias vezes: cada novo aprendizado melhora o anterior, "
                            + "até que o gatinho fique muito bom em reconhecer brinquedos.",
                        "Entendi",
                        () =>
                        {
                            string chaveNegativa = obj.name + "_" + nomeCor;
                            objetosNegativos.Add(chaveNegativa);

                            if (balaoPensamento != null)
                                balaoPensamento.SetActive(true);
                            if (pensamento != null)
                                pensamento.text =
                                    $"Vou lembrar que {obj.name} ({nomeCor}) <b>não</b> é um brinquedo.";

                            StartCoroutine(EsconderBalaoEContinuar());
                            emTreinamentoExtra = false;

                            explicacaoExtraMostrada = true;
                        }
                    );
                }
            );
        }
        else
        {
            string chaveNegativa = obj.name + "_" + nomeCor;
            objetosNegativos.Add(chaveNegativa);

            if (balaoPensamento != null)
                balaoPensamento.SetActive(true);
            if (pensamento != null)
                pensamento.text =
                    $"Vou lembrar que {obj.name} ({nomeCor}) <b>não</b> é um brinquedo.";

            StartCoroutine(EsconderBalaoEContinuar());
            emTreinamentoExtra = false;
        }
    }
    #endregion

    #region Aprender Nova Cor
    private void AprenderNovaCor(string nomeCor, bool ehBrinquedo, string nomeObjeto = "")
    {
        if (string.IsNullOrEmpty(nomeCor))
            return;

        SpriteRenderer sr = objetoAtual?.GetComponent<SpriteRenderer>();
        Color corObjeto = sr != null ? sr.color : Color.clear;

        if (ehBrinquedo)
        {
            if (!coresAprendidasUnicas.Contains(corObjeto))
                coresAprendidasUnicas.Add(corObjeto);

            if (!brinquedosAprendidos.Any(c => CoresSemelhantes(c, corObjeto)))
                brinquedosAprendidos.Add(corObjeto);

            if (pensamento != null)
                pensamento.text = $"Aprendi que {nomeObjeto} da cor {nomeCor} é um brinquedo!";
        }
        else
        {
            if (!string.IsNullOrEmpty(nomeObjeto))
            {
                string chave = nomeObjeto + "_" + nomeCor;
                if (!objetosNegativos.Contains(chave))
                    objetosNegativos.Add(chave);
            }

            if (pensamento != null)
                pensamento.text = $"Entendi! {nomeObjeto} da cor {nomeCor} <b>não</b> é brinquedo.";
        }

        StartCoroutine(EsconderPensamento());
    }

    #endregion

    #region Esconder Pensamento
    private IEnumerator EsconderPensamento()
    {
        yield return _waitForSeconds2;

        if (balaoPensamento != null)
            balaoPensamento.SetActive(false);
    }
    #endregion

    private void MostrarPopUpAmostragem()
    {
        if (popUpController == null)
            return;

        popUpController.Mostrar(
            "Amostragem Limitada",
            "No jogo, o gatinho aprendeu com poucos exemplos, isso é uma <b>amostragem limitada</b>.<br>"
                + "Mesmo assim, ele conseguiu melhorar, mas sua memória pode não ser perfeita.<br>"
                + "Na prática, em aprendizado de máquina, precisamos de <b>muitos exemplos</b> "
                + "(milhares ou até milhões!) para que o modelo seja realmente eficiente "
                + "e consiga generalizar para novos casos.",
            "Entendi",
            () =>
            {
                explorando = true;
            }
        );
    }

    private void MostrarPopUpConclusao()
    {
        if (popUpController == null)
            return;

        explorando = false;

        popUpController.Mostrar(
            "Conclusão",
            "Parabéns! Você ajudou o gatinho a aprender como classificar brinquedos.<br>"
                + "Assim como o CatBoost, ele usou <b>exemplos positivos</b> e <b>negativos</b>, "
                + "foi corrigindo seus erros e agora consegue prever melhor.<br>"
                + "Isso é <b>aprendizado supervisionado</b> na prática!",
            "Concluir",
            () => { }
        );
    }

    #region Auxiliares
    bool CoresSemelhantes(Color a, Color b, float tolerancia = 0.1f)
    {
        return Mathf.Abs(a.r - b.r) < tolerancia
            && Mathf.Abs(a.g - b.g) < tolerancia
            && Mathf.Abs(a.b - b.b) < tolerancia;
    }

    string NomeDaCor(Color cor)
    {
        if (cor == Color.red)
            return "vermelho";
        if (cor == Color.green)
            return "verde";
        if (cor == Color.blue)
            return "azul";
        if (cor == Color.yellow)
            return "amarelo";
        if (cor == Color.white)
            return "branco";
        if (cor == Color.black)
            return "preto";
        if (cor == Color.gray)
            return "cinza";
        if (cor == Color.magenta)
            return "magenta";
        if (cor == Color.cyan)
            return "ciano";

        float r = cor.r,
            g = cor.g,
            b = cor.b;
        if (r > 0.6f && g < 0.4f && b < 0.4f)
            return "vermelho";
        if (g > 0.6f && r < 0.4f && b < 0.4f)
            return "verde";
        if (b > 0.6f && r < 0.4f && g < 0.4f)
            return "azul";
        if (r > 0.5f && g > 0.3f && b < 0.2f)
            return "laranja";
        if (r > 0.5f && b > 0.5f && g < 0.4f)
            return "roxo";

        return "Uma cor desconhecida";
    }
    #endregion
}
