using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class GatinhoAprendiz : MonoBehaviour
{
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
    private List<Color> brinquedosAprendidos = new List<Color>();
    public int quantidadeDePontos = 10;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        GerarPontosNosBrinquedos();
        if (popUpController == null) popUpController = Object.FindFirstObjectByType<PopUpController>(FindObjectsInactive.Include);
        if (popUpController != null)
        {
            popUpController.gameObject.SetActive(true); // ativa o objeto raiz se estava desativado 
            popUpController.Mostrar("Bem-vindo ao Gatinho Classificador!<br>" + "Arraste os brinquedos até o gatinho para ensiná-lo quais são os brinquedos!<br>" + "Depois, ele tentará identificar sozinho os brinquedos.<br>" + "Confirme se ele acertou ou errou para ajudá-lo a aprender!",
            "Jogar", () => MostrarExplicacaoAlgoritmo());
        }
        else { Debug.LogError("PopUpController não foi encontrado na cena!"); }
    }

    void Update()
    {
        MovimentarGatinho();
        AtualizarPosicaoBalaoPensamento();

        if (animator != null)
            animator.SetBool("explorando", explorando);
    }
    #endregion

    #region Explicações Extras
    void MostrarExplicacaoAlgoritmo()
    {
        if (popUpController == null) popUpController = Object.FindFirstObjectByType<PopUpController>(FindObjectsInactive.Include);

        if (popUpController != null)
        {
            popUpController.Mostrar(
                "Nos bastidores, este jogo simula um algoritmo de aprendizado supervisionado: <b>CatBoost Classifier</b>.<br>" +
                "Ele funciona analisando exemplos rotulados (os brinquedos que você mostra) " +
                "e criando um modelo para prever se novos objetos também são brinquedos.<br>" +
                "Aqui, simplificamos o processo usando as cores dos objetos como atributos principais.",
                "Entendi", () => { popUpController.Fechar(); }
            );
        }
    }
    #endregion

    #region UI Control
    public void JogadorValidou(bool acertou)
    {
        if (balaoPensamento != null)
            balaoPensamento.SetActive(true);

        if (pensamento != null)
            pensamento.text = acertou ? "Miauu! Acertei!" : "Oh não errei...";

        StartCoroutine(EsconderBalaoEContinuar());
    }

    IEnumerator EsconderBalaoEContinuar()
    {
        yield return new WaitForSeconds(2f);

        if (balaoPensamento != null)
            balaoPensamento.SetActive(false);

        if (pontosExploracaoDinamicos.Count > 0)
            indexExploracao = (indexExploracao + 1) % pontosExploracaoDinamicos.Count;

        explorando = true;
    }
    #endregion

    #region Movimentação
    HashSet<GameObject> objetosExplorados = new HashSet<GameObject>();


    private void MovimentarGatinho()
    {
        if (!explorando || pontosExploracaoDinamicos.Count == 0)
            return;

        Transform alvo = pontosExploracaoDinamicos[indexExploracao];
        if (alvo == null) return;

        // move em direção ao ponto
        transform.position = Vector2.MoveTowards(transform.position, alvo.position, velocidade * Time.deltaTime);

        // usa sempre VirarParaObjeto, mesmo para ponto
        VirarParaObjeto(alvo.gameObject);

        // chegou no ponto?
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

                // vira exatamente para o objeto escolhido
                VirarParaObjeto(maisProximo);

                StartCoroutine(VerificarObjeto(maisProximo));
                explorando = false;
                return;
            }

            // avança para próximo ponto
            if (pontosExploracaoDinamicos.Count > 0)
                indexExploracao = (indexExploracao + 1) % pontosExploracaoDinamicos.Count;
        }

    }

    void VirarParaObjeto(GameObject obj)
    {
        if (obj == null) return;

        float direcaoX = obj.transform.position.x - transform.position.x;

        if (direcaoX > 0.01f)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (direcaoX < -0.01f)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    private void AtualizarPosicaoBalaoPensamento()
    {
        if (balaoPensamento != null && balaoPensamento.activeSelf)
        {
            if (Camera.main != null)
            {
                Vector3 posTela = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 2f, 0));
                balaoPensamento.transform.position = posTela;
            }
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

            GameObject ponto = new GameObject("PontoExploracao_" + brinquedo.name);
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

    IEnumerator FazerAnalise(GameObject obj)
    {
        VirarParaObjeto(obj);

        if (balaoPensamento != null) balaoPensamento.SetActive(true);
        if (pensamento != null) pensamento.text = "Deixe-me observar esse objeto...";
        yield return new WaitForSeconds(2f);

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        string nomeCor = "cor indefinida";
        Color corObjeto = Color.clear;

        if (sr != null)
        {
            corObjeto = sr.color;
            nomeCor = NomeDaCor(corObjeto);
            brinquedosAprendidos.Add(corObjeto);
        }

        if (pensamento != null)
            pensamento.text = $"Entendi! Vou lembrar que o {obj.name} é um brinquedo de cor {nomeCor}.";
        yield return new WaitForSeconds(2f);

        obj.GetComponent<DragDrop>()?.VoltarPosicaoInicial();

        // desativa balão antes de abrir pop-up
        if (balaoPensamento != null) balaoPensamento.SetActive(false);

        // captura 'obj' na closure para evitar usar objetoAtual (que pode mudar)
        GameObject objetoCapturado = obj;

        if (popUpController != null)
        {
            popUpController.Mostrar(
                "Deseja mostrar outro brinquedo ao gatinho?",
                "Sim", () => StartCoroutine(FazerAnalise(objetoCapturado)),
                "Não", () => { explorando = true; }
            );
        }
        else
        {
            Debug.LogWarning("[GatinhoAprendiz] popUpController não atribuído — pulando pop-up de continuação.");
            explorando = true;
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

        if (achouCor)
        {
            if (pensamento != null) pensamento.text = $"Lembro que já vi brinquedos dessa cor ({nomeCor})...";
            yield return new WaitForSeconds(2f);
            if (pensamento != null) pensamento.text = $"Então acredito que o {obj.name} seja um brinquedo!";
        }
        else
        {
            if (pensamento != null) pensamento.text = $"Hmm... não me lembro de ter visto essa cor antes ({nomeCor}).";
            yield return new WaitForSeconds(2f);
            if (pensamento != null) pensamento.text = $"Será que o {obj.name} também é um brinquedo?";
        }

        yield return new WaitForSeconds(2f);

        // esconde o balão antes do pop-up
        if (balaoPensamento != null) balaoPensamento.SetActive(false);

        if (popUpController != null)
        {
            // captura obj para closure
            GameObject objetoCapturado = obj;

            popUpController.Mostrar(
               $"O gatinho acha que {obj.name} é um brinquedo. Ele acertou?",
               "Sim", () => JogadorValidou(true),
               "Não", () => JogadorValidou(false)
           );
        }
        else
        {
            Debug.LogWarning("[GatinhoAprendiz] popUpController não atribuído — não será possível validar via pop-up.");
            JogadorValidou(false);
        }
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

        float r = cor.r;
        float g = cor.g;
        float b = cor.b;

        if (r > 0.6f && g < 0.4f && b < 0.4f) return "vermelho";
        if (g > 0.6f && r < 0.4f && b < 0.4f) return "verde";
        if (b > 0.6f && r < 0.4f && g < 0.4f) return "azul";
        if (r > 0.5f && g > 0.3f && b < 0.2f) return "laranja";
        if (r > 0.5f && b > 0.5f && g < 0.4f) return "roxo";

        return "Uma cor desconhecida";
    }
    #endregion
}
