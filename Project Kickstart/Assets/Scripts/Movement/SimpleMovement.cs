using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleMovement : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float velocidade = 6f;
    public float suavidade = 10f;
    public bool usarCameraRelativo = true;
    
    [Header("Configurações de Pulo")]
    public float forcaPulo = 7f;
    public float distanciaChao = 0.2f;
    public LayerMask mascaraChao = 1;
    public Transform verificadorChao;
    
    [Header("Configurações de Rotação")]
    public float velocidadeRotacao = 10f;
    public bool rotacionarParaMovimento = true;
    
    [Header("Referências")]
    [SerializeField] private Animator animator;
    
    // Componentes privados
    private Rigidbody rb;
    private Camera cameraJogador;
    private bool estaNoChao;
    
    // Variáveis de movimento
    private Vector3 direcaoMovimento;
    private Vector3 velocidadeAtual;
    
    void Start()
    {
        // Obter componentes necessários
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Evita que o rigidbody gire sozinho
        
        // Procurar pela câmera principal se usarCameraRelativo estiver ativado
        if (usarCameraRelativo)
        {
            cameraJogador = Camera.main;
            if (cameraJogador == null)
                cameraJogador = FindObjectOfType<Camera>();
        }
        
        // Se não tem verificador de chão, criar um
        if (verificadorChao == null)
        {
            GameObject verificador = new GameObject("VerificadorChao");
            verificador.transform.SetParent(transform);
            verificador.transform.localPosition = Vector3.down * 0.5f;
            verificadorChao = verificador.transform;
        }
    }
    
    void Update()
    {
        // Verificar se está no chão
        estaNoChao = Physics.CheckSphere(verificadorChao.position, distanciaChao, mascaraChao);
        
        // Capturar input do jogador
        CapturarInput();
        
        // Pular
        if (Input.GetKeyDown(KeyCode.Space) && estaNoChao)
        {
            Pular();
        }
        
        // Atualizar animações
        AtualizarAnimacoes();
    }
    
    void FixedUpdate()
    {
        // Aplicar movimento
        AplicarMovimento();
        
        // Rotacionar personagem se necessário
        if (rotacionarParaMovimento && direcaoMovimento.magnitude > 0.1f)
        {
            RotacionarPersonagem();
        }
    }
    
    void CapturarInput()
    {
        // Capturar input horizontal e vertical (WASD ou setas)
        float horizontal = Input.GetAxis("Horizontal"); // A/D ou setas esquerda/direita
        float vertical = Input.GetAxis("Vertical");     // W/S ou setas cima/baixo
        
        // Criar vetor de direção baseado no input
        Vector3 inputDirecao = new Vector3(horizontal, 0f, vertical);
        
        // Se usar movimento relativo à câmera
        if (usarCameraRelativo && cameraJogador != null)
        {
            // Obter direções da câmera (ignorando rotação Y)
            Vector3 cameraFrente = cameraJogador.transform.forward;
            Vector3 cameraDireita = cameraJogador.transform.right;
            
            // Projetar no plano horizontal (Y = 0)
            cameraFrente.y = 0f;
            cameraDireita.y = 0f;
            
            // Normalizar
            cameraFrente.Normalize();
            cameraDireita.Normalize();
            
            // Calcular direção final baseada na câmera
            direcaoMovimento = (cameraFrente * vertical + cameraDireita * horizontal).normalized;
        }
        else
        {
            // Movimento relativo ao mundo (padrão Unity)
            direcaoMovimento = inputDirecao.normalized;
        }
    }
    
    void AplicarMovimento()
    {
        // Calcular velocidade alvo
        Vector3 velocidadeAlvo = direcaoMovimento * velocidade;
        
        // Manter a velocidade Y atual (para não interferir na gravidade/pulo)
        velocidadeAlvo.y = rb.linearVelocity.y;
        
        // Aplicar movimento suave
        Vector3 velocidadeSuave = Vector3.Lerp(rb.linearVelocity, velocidadeAlvo, suavidade * Time.fixedDeltaTime);
        rb.linearVelocity = velocidadeSuave;
        
        // Atualizar velocidade atual para animações
        velocidadeAtual = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }
    
    void RotacionarPersonagem()
    {
        // Criar rotação alvo baseada na direção do movimento
        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcaoMovimento);
        
        // Aplicar rotação suave
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeRotacao * Time.fixedDeltaTime);
    }
    
    void Pular()
    {
        // Aplicar força de pulo
        rb.AddForce(Vector3.up * forcaPulo, ForceMode.Impulse);
    }
    
    void AtualizarAnimacoes()
    {
        if (animator != null)
        {
            // Calcular velocidade atual
            float velocidadeAtualMagnitude = velocidadeAtual.magnitude;
            
            // Definir threshold para animação de corrida
            float thresholdCorrida = 0.5f;
            bool estaCorrendo = velocidadeAtualMagnitude > thresholdCorrida;
            
            // Atualizar parâmetros do animator
            animator.SetBool("IsRunning", estaCorrendo);
            animator.SetFloat("Speed", velocidadeAtualMagnitude);
        }
    }
    
    // Desenhar gizmos para debug
    void OnDrawGizmosSelected()
    {
        // Desenhar esfera do verificador de chão
        if (verificadorChao != null)
        {
            Gizmos.color = estaNoChao ? Color.green : Color.red;
            Gizmos.DrawWireSphere(verificadorChao.position, distanciaChao);
        }
        
        // Desenhar direção do movimento
        if (Application.isPlaying && direcaoMovimento.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, direcaoMovimento * 2f);
        }
    }
}
