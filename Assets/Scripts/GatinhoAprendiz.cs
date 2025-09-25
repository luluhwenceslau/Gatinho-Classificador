using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class GatinhoAprendiz : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds2 = new(2f);
    #region Singleton
    public static GatinhoAprendiz Instance;
    void Awake() { Instance = this; }
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
    private List<Transform> pontosExploracaoDinamicos = new List<Transform>();
    #endregion

    #region Game State
    private GameObject objetoAtual;
    private List<Color> brinquedosAprendidos = new();
    private HashSet<string> nomesNaoBrinquedos = new();
    private HashSet<string> objetosNegativos = new();
    private int brinquedosMostrados = 0;
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
                "Bem-vindo ao Gatinho Classificador!<br><br>" +
                "Você deve mostrar <b>2 brinquedos de cores diferentes</b> ao gatinho para que ele aprenda a identificá-los.",
                "Continuar", () =>
                {
                    popUpController.Mostrar(
                        "Depois disso, ele tentará identificar sozinho os brinquedos. " +
                        "Você dirá se ele acertou ou errou, ajudando-o a aprender!<br>",
                        "Vamos começar!", () =>
                        {
                            popUpController.Fechar();
                            explorando = true; // libera o jogador para mostrar os brinquedos
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
    }
    #endregion

    #region UI Control
    public void JogadorValidou(bool acertou)
    {
        if (balaoPensamento != null)
            balaoPensamento.SetActive(true);

        if (pensamento != null)
            pensamento.text = acertou ? "Miauu! Acertei!" : "Oh não... errei.";

        if (!acertou && objetoAtual != null)
        {
            string nome = objetoAtual.name;
            if (!nomesNaoBrinquedos.Contains(nome))
            {
                nomesNaoBrinquedos.Add(nome);
                Debug.Log($"[Treino Extra] O objeto {nome} foi marcado como NÃO brinquedo.");
            }
        }

        StartCoroutine(EsconderBalaoEContinuar());
    }

    IEnumerator EsconderBalaoEContinuar()
    {
        yield return _waitForSeconds2;

        if (balaoPensamento != null)
            balaoPensamento.SetActive(false);

        if (pontosExploracaoDinamicos.Count > 0)
            indexExploracao = (indexExploracao + 1) % pontosExploracaoDinamicos.Count;

        explorando = true;
    }
    #endregion

    #region Movimentação
    readonly HashSet<GameObject> objetosExplorados = new();

    private void MovimentarGatinho()
    {
        if (!explorando || pontosExploracaoDinamicos.Count == 0)
            return;

        Transform alvo = pontosExploracaoDinamicos[indexExploracao];
        if (alvo == null) return;

        transform.position = Vector2.MoveTowards(transform.position, alvo.position, velocidade * Time.deltaTime);

        VirarParaObjeto(alvo.gameObject);

        if (Vector2.Distance(transform.position, alvo.position) < 0.3f)
        {
            Collider2D[] col = Physics2D.OverlapCircleAll(alvo.position, 0.8f);

            GameObject maisProximo = null;
            float menorDistancia = Mathf.Infinity;

            foreach (var c in col)
            {
                if (c != null && c.CompareTag("ObjetoAnalise") && !objetosExplorados.Contains(c.gameObject))
                {
                    float dist = Vector2.Distance(transform.position, c.transform.position);
                    if (dist < menorDistancia)
                    {
                        menorDistancia = dist;
                        maisProximo = c.gameObject;
                    }
                }
            }

            if (maisProximo != null)
            {
                objetosExplorados.Add(maisProximo);

                VirarParaObjeto(maisProximo);

                StartCoroutine(VerificarObjeto(maisProximo));
                explorando = false;
                return;
            }

            if (pontosExploracaoDinamicos.Count > 0)
                indexExploracao = (indexExploracao + 1) % pontosExploracaoDinamicos.Count;
        }
    }

    void VirarParaObjeto(GameObject obj)
    {
        if (obj == null) return;

        float direcaoX = obj.transform.position.x - transform.position.x;
        if (direcaoX > 0.01f)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (direcaoX < -0.01f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void AtualizarPosicaoBalaoPensamento()
    {
        if (balaoPensamento != null && balaoPensamento.activeSelf && Camera.main != null)
        {
            Vector3 posTela = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 2f, 0));
            balaoPensamento.transform.position = posTela;
        }
    }
    #endregion

    #region Exploração e Análise
    void GerarPontosNosBrinquedos()
    {
        pontosExploracaoDinamicos.Clear();

        GameObject[] brinquedos = GameObject.FindGameObjectsWithTag("ObjetoAnalise");
        foreach (GameObject brinquedo in brinquedos)
        {
            if (brinquedo == null) continue;

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
        if (obj == null) return;

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
        if (obj == null) yield break;
        objetoAtual = obj;

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        string nomeCor = "cor indefinida";
        Color cor = Color.clear;
        bool achouCor = false;

        if (sr != null)
        {
            cor = sr.color;
            nomeCor = NomeDaCor(cor);
            achouCor = brinquedosAprendidos.Any(c => CoresSemelhantes(c, cor));
        }

        if (balaoPensamento != null) balaoPensamento.SetActive(true);

        if (achouCor && !nomesNaoBrinquedos.Contains(obj.name))
        {
            if (pensamento != null) pensamento.text = $"Já vi brinquedos dessa cor ({nomeCor})...";
            yield return _waitForSeconds2;
            if (pensamento != null) pensamento.text = $"Acredito que esse {obj.name} seja um brinquedo!";
        }
        else
        {
            if (pensamento != null) pensamento.text = $"Hmm... não conheço esse {obj.name}.";
            yield return _waitForSeconds2;
            if (pensamento != null) pensamento.text = $"Acho que não é brinquedo!";
        }

        yield return _waitForSeconds2;

        if (balaoPensamento != null) balaoPensamento.SetActive(false);

        if (popUpController != null)
        {
            GameObject objetoCapturado = obj;
            popUpController.Mostrar(
               $"O gatinho acha que {obj.name} é um brinquedo. Ele acertou?",
               "Sim", () => JogadorValidou(true),
               "Não", () => JogadorValidou(false)
           );
        }
        else JogadorValidou(false);
    }
    #endregion

    #region Treinamento

    IEnumerator FazerAnalise(GameObject obj)
    {

        if (balaoPensamento != null) balaoPensamento.SetActive(true);
        if (pensamento != null) pensamento.text = "Deixe-me observar esse objeto...";
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
            pensamento.text = $"Entendi! Se for {nomeCor} é um brinquedo.";
        yield return _waitForSeconds2;

        obj.GetComponent<DragDrop>()?.VoltarPosicaoInicial();
        if (balaoPensamento != null) balaoPensamento.SetActive(false);

        if (coresAprendidasUnicas.Count < 2)
        {
            if (popUpController != null)
            {
                popUpController.Mostrar(
                    "Mostre outro brinquedo de cor diferente ao gatinho.",
                    "Ok", () => { });
            }
        }
        else
        {
            if (popUpController != null)
            {
                popUpController.Mostrar(
                    "Deseja mostrar outro brinquedo ao gatinho?",
                    "Sim", () => StartCoroutine(FazerAnalise(obj)),
                    "Não", () => { explorando = true; });
            }
            else explorando = true;
        }
    }
    #endregion

    #region Treinamento Extra
    private void IniciarTreinamentoExtra(GameObject obj)
    {
        if (obj == null) return;

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        string nomeCor = "cor indefinida";
        Color cor = Color.clear;

        popUpController.Mostrar(
            "Ops! O gatinho errou.<br><br>" +
            "Agora ele vai aprender que este objeto NÃO é um brinquedo.<br>" +
            "No aprendizado de máquina, isso é chamado de <b>exemplo negativo</b>.<br><br>" +
            "A partir de agora, ele vai comparar não só a cor, mas também o <b>nome</b> do objeto para evitar novos erros.",
            "Continuar",
            () =>
            {
                string chaveNegativa = obj.name + "_" + nomeCor;
                objetosNegativos.Add(chaveNegativa);

                Debug.Log($"Treino extra: {chaveNegativa} registrado como NÃO brinquedo.");

                explorando = true;
            }
        );
    }
    #endregion

    #region Auxiliares
    bool CoresSemelhantes(Color a, Color b, float tolerancia = 0.1f)
    {
        return Mathf.Abs(a.r - b.r) < tolerancia &&
               Mathf.Abs(a.g - b.g) < tolerancia &&
               Mathf.Abs(a.b - b.b) < tolerancia;
    }

    string NomeDaCor(Color cor)
    {
        if (cor == Color.red) return "vermelho";
        if (cor == Color.green) return "verde";
        if (cor == Color.blue) return "azul";
        if (cor == Color.yellow) return "amarelo";
        if (cor == Color.white) return "branco";
        if (cor == Color.black) return "preto";
        if (cor == Color.gray) return "cinza";
        if (cor == Color.magenta) return "magenta";
        if (cor == Color.cyan) return "ciano";

        float r = cor.r, g = cor.g, b = cor.b;
        if (r > 0.6f && g < 0.4f && b < 0.4f) return "vermelho";
        if (g > 0.6f && r < 0.4f && b < 0.4f) return "verde";
        if (b > 0.6f && r < 0.4f && g < 0.4f) return "azul";
        if (r > 0.5f && g > 0.3f && b < 0.2f) return "laranja";
        if (r > 0.5f && b > 0.5f && g < 0.4f) return "roxo";

        return "Uma cor desconhecida";
    }
    #endregion
}
