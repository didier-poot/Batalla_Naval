using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class BatallaNavalGUI : MonoBehaviour
{
    public LogicaBot botEnemigo;
    
    [Header("Paneles de Navegación")]
    public GameObject panelMenuInicio;
    public GameObject panelJuego;

    [Header("Campos de Menú")]
    public TMP_InputField inputNombreJugador;
    public TMP_Text labelBienvenida;

    [Header("Referencias de Tableros (UI)")]
    public Transform gridJugador;  // "Tu Flota"
    public Transform gridEnemigo;  // "Radar Enemigo"
    public GameObject botonCasillaPrefab;
    public TMP_Text labelEstado;
    public TMP_Text labelResultados;

    [Header("Assets Visuales")]
    public Sprite spriteAgua;      // Tile de agua del Pirate Pack
    public Sprite spriteImpacto;   // Explosión del Pixel Art FX
    public Sprite spriteFallo;     // Salpicadura de agua o "X"
    [System.Serializable]
    public struct DiseñoBarco
    {
        public Sprite proa;
        public Sprite cuerpo;
        public Sprite popa;
    }

    // 2. Creamos una lista de estos diseños para tu flota entera
    [Header("Diseños de la Flota")]
    public DiseñoBarco[] diseñosFlota;

    // Lógica de Doble Tablero 
    private int[,] tableroJugador = new int[10, 10];
    
    // Matrices visuales
    private Image[,] visualJugador = new Image[10, 10];
    private Image[,] visualEnemigo = new Image[10, 10];

    // Configuración de Flota
    private int[] tamaniosBarcos = { 5, 4, 3, 3, 2 }; // Portaviones, Acorazado, etc.
    private int indiceBarcoActual = 0;
    private bool esHorizontal = true;
    private string nombreAlmirante = "Almirante";

    public enum EstadoJuego { Menu, FaseColocacion, TurnoJugador, TurnoEnemigo, FinJuego }
    private EstadoJuego estadoActual;
    [Header("Fin de Juego")]
    public GameObject panelFinJuego;
    public TMP_Text textoFinJuego;
    
    // 5 + 4 + 3 + 3 + 2 = 17 impactos para destruir toda la flota
    private int impactosParaGanar = 17; 
    private int aciertosJugador = 0;
    private int aciertosBot = 0;

    void Start()
    {
        panelMenuInicio.SetActive(true);
        panelJuego.SetActive(false);
        estadoActual = EstadoJuego.Menu;
    }

    void Update()
    {
        // Alternar rotación del barco con Clic Derecho durante la colocación
        if (estadoActual == EstadoJuego.FaseColocacion && Input.GetMouseButtonDown(1))
        {
            esHorizontal = !esHorizontal;
            ActualizarMensajeEstado();
        }

        // Reinicio rápido al final del juego con una tecla
        if (estadoActual == EstadoJuego.FinJuego && Input.GetKeyDown(KeyCode.R))
        {
            ReiniciarPartida();
        }
    }

    // Método conectado al botón "INICIAR SECUENCIA"
    public void IniciarJuego()
    {
        if (!string.IsNullOrEmpty(inputNombreJugador.text))
            nombreAlmirante = inputNombreJugador.text;

        labelBienvenida.text = $"Base del Almirante: {nombreAlmirante}";

        panelMenuInicio.SetActive(false);
        panelJuego.SetActive(true);

        GenerarTablerosUI();
        CambiarEstado(EstadoJuego.FaseColocacion);
    }

   private void GenerarTablerosUI()
{
    for (int x = 0; x < 10; x++)
    {
        for (int y = 0; y < 10; y++)
        {
            // Crear casilla en "Tu Flota"
            GameObject bJugador = Instantiate(botonCasillaPrefab, gridJugador);
            visualJugador[x, y] = bJugador.GetComponent<Image>();
            
            // Asigna la textura del agua al jugador
            visualJugador[x, y].sprite = spriteAgua; 
            
            int posX = x, posY = y;
            bJugador.GetComponent<Button>().onClick.AddListener(() => AlClicTableroJugador(posX, posY));

            // Crear casilla en "Radar Enemigo"
            GameObject bEnemigo = Instantiate(botonCasillaPrefab, gridEnemigo);
            visualEnemigo[x, y] = bEnemigo.GetComponent<Image>();
            
            // Asigna la textura del agua al enemigo
            visualEnemigo[x, y].sprite = spriteAgua; 
            
            bEnemigo.GetComponent<Button>().onClick.AddListener(() => AlClicTableroEnemigo(posX, posY));
        }
    }
}

    private void AlClicTableroJugador(int x, int y)
    {
        if (estadoActual == EstadoJuego.FaseColocacion)
        {
            int tamanio = tamaniosBarcos[indiceBarcoActual];
            if (PuedeColocarBarco(tableroJugador, x, y, tamanio, esHorizontal))
            {
                ColocarBarco(tableroJugador, visualJugador, x, y, tamanio, esHorizontal);
                indiceBarcoActual++;

                if (indiceBarcoActual >= tamaniosBarcos.Length)
                {
                    CambiarEstado(EstadoJuego.TurnoJugador);
                }
                else
                {
                    ActualizarMensajeEstado();
                }
            }
        }
    }

    // Único método AlClicTableroEnemigo integrado con tu sistema de turnos
    public void AlClicTableroEnemigo(int x, int y)
    {
        // Solo permite clics si es el turno del jugador
        if (estadoActual != EstadoJuego.TurnoJugador) return;

        // 1. Verificamos si ya habías disparado en esta coordenada 
        // (-1 es agua golpeada, -2 es barco golpeado)
        if (botEnemigo.tableroEnemigo[x, y] == -2 || botEnemigo.tableroEnemigo[x, y] == -1)
        {
            return; // Ya disparaste aquí, ignoramos el clic
        }
        
        // Traducimos tus coordenadas
        string coordTexto = TraducirCoordenadas(x, y);
        
        // Obtenemos qué hay en la casilla
        int idImpacto = botEnemigo.tableroEnemigo[x, y];

        // 2. Revisamos si hay barco (ID mayor a 0) o agua (0) en la lógica del bot
        if (idImpacto > 0) 
        {
            visualEnemigo[x, y].sprite = spriteImpacto; 
            visualEnemigo[x, y].color = Color.white;

            botEnemigo.tableroEnemigo[x, y] = -2; // Lo marcamos como golpeado (-2)
            
            // Restamos 1 punto de vida al barco específico que tocamos
            botEnemigo.vidaBarcos[idImpacto]--;

            // Verificamos si la vida de ese barco llegó a cero
            if (botEnemigo.vidaBarcos[idImpacto] <= 0)
            {
                string nombre = botEnemigo.nombresBarcos[idImpacto];
                labelResultados.text = $"¡Hundiste el {nombre} enemigo en {coordTexto}!";
            }
            else
            {
                labelResultados.text = $"¡Tiro Certero! Impactaste un barco enemigo en {coordTexto}.";
            }
            
            aciertosJugador++;
            if (aciertosJugador >= impactosParaGanar)
            {
                MostrarPantallaFinal("¡VICTORIA ALMIRANTE!\nHundiste toda la flota enemiga.");
                return; // Detenemos la función para que el bot no contraataque
            }
        }
        else if (idImpacto == 0) 
        {
            visualEnemigo[x, y].sprite = spriteFallo; 
            visualEnemigo[x, y].color = Color.white; 
            botEnemigo.tableroEnemigo[x, y] = -1;    
            labelResultados.text = $"Fallaste. Disparo al agua en {coordTexto}.";
        }
        // 3. Cambiamos al turno del enemigo (inicia la corrutina de espera)
        CambiarEstado(EstadoJuego.TurnoEnemigo);
    }

    private bool PuedeColocarBarco(int[,] tablero, int x, int y, int tamanio, bool horizontal)
    {
        for (int i = 0; i < tamanio; i++)
        {
            int nx = horizontal ? x : x + i;
            int ny = horizontal ? y + i : y;

            if (nx >= 10 || ny >= 10 || tablero[nx, ny] != 0)
                return false;
        }
        return true;
    }

    private void ColocarBarco(int[,] tablero, Image[,] visual, int x, int y, int tamanio, bool horizontal)
    {
        // Tomamos el diseño que le toca al barco actual
        DiseñoBarco disenoActual = diseñosFlota[indiceBarcoActual];

        for (int i = 0; i < tamanio; i++)
        {
            int nx = horizontal ? x : x + i;
            int ny = horizontal ? y + i : y;

            tablero[nx, ny] = 1; // 1 = Barco
            
            // Buscamos la capa superior (IconoContenido)
            Image capaSuperior = visual[nx, ny].transform.GetChild(0).GetComponent<Image>();
            
            // Asignamos el sprite según la posición de la pieza
            if (i == 0) 
            {
                capaSuperior.sprite = disenoActual.proa;
            }
            else if (i == tamanio - 1) 
            {
                capaSuperior.sprite = disenoActual.popa;
            }
            else 
            {
                capaSuperior.sprite = disenoActual.cuerpo;
            }
            
            // Restauramos la transparencia para que se vea
            capaSuperior.color = new Color(1, 1, 1, 1); 

            // Rotamos la imagen para que el barco apunte hacia donde debe
            // (Los sprites del Pirate Pack miran hacia arriba por defecto)
            if (horizontal)
            {
                // Giramos 90 grados para que se acueste
                capaSuperior.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            }
            else
            {
                // Lo dejamos normal
                capaSuperior.rectTransform.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }

    private void CambiarEstado(EstadoJuego nuevoEstado)
    {
        estadoActual = nuevoEstado;
        ActualizarMensajeEstado();

        if (estadoActual == EstadoJuego.TurnoEnemigo)
            StartCoroutine(SimularTurnoEnemigo());
    }

    private void ActualizarMensajeEstado()
    {
        switch (estadoActual)
        {
            case EstadoJuego.FaseColocacion:
                string orientacion = esHorizontal ? "Horizontal" : "Vertical";
                labelEstado.text = $"Coloca barco de {tamaniosBarcos[indiceBarcoActual]} casillas ({orientacion}) - Clic Der. rotar";
                break;
            case EstadoJuego.TurnoJugador:
                labelEstado.text = "¡Selecciona una casilla en el Radar Enemigo para atacar!";
                break;
            case EstadoJuego.TurnoEnemigo:
                labelEstado.text = "El enemigo está calculando su disparo...";
                break;
            case EstadoJuego.FinJuego:
                labelEstado.text = "Partida finalizada";
                break;
        }
    }

    private IEnumerator SimularTurnoEnemigo()
    {
        // El bot "piensa" por 1.5 segundos
        yield return new WaitForSeconds(1.5f);
        
        // El bot dispara
        RecibirDisparoDelBot();

        if (estadoActual == EstadoJuego.FinJuego)
            yield break;

        // Se le devuelve el turno al jugador
        CambiarEstado(EstadoJuego.TurnoJugador);
    }

    // Único método de disparo del bot usando la matriz visual
    private void RecibirDisparoDelBot()
{
    Vector2Int coordenadasAtaque = botEnemigo.TurnoDeAtaqueBot();
    int botX = coordenadasAtaque.x;
    int botY = coordenadasAtaque.y;

    // Traducimos las coordenadas antes de imprimirlas
    string coordTexto = TraducirCoordenadas(botX, botY);

    // Obtenemos la capa superior (IconoContenido) de la casilla atacada
    Image capaSuperior = visualJugador[botX, botY].transform.GetChild(0).GetComponent<Image>();

    // 1. Evaluamos la matriz numerica en lugar de comparar sprites
    if (tableroJugador[botX, botY] == 1) // 1 = Hay barco
    {
        capaSuperior.sprite = spriteImpacto;
        capaSuperior.color = Color.white; // Asegura que el Alpha sea 1 para que se vea la explosion
        tableroJugador[botX, botY] = 2;   // 2 = Casilla con barco destruido

        labelResultados.text = $"¡Alerta! El enemigo impactó tu barco en {coordTexto}.";
        aciertosBot++;
        
        if (aciertosBot >= impactosParaGanar)
        {
            MostrarPantallaFinal("¡DERROTA!\nEl enemigo destruyó tu flota.");
        }
    }
    else
    {
        capaSuperior.sprite = spriteFallo;
        capaSuperior.color = Color.white;
        tableroJugador[botX, botY] = 3;   // 3 = Agua disparada / Fallo

        labelResultados.text = $"El enemigo disparó en {coordTexto} y cayó al agua.";
    }
}
    private void ReiniciarPartida()
    {
        // Reinicia el estado del juego
        tableroJugador = new int[10, 10];
        indiceBarcoActual = 0;
        esHorizontal = true;
        nombreAlmirante = string.IsNullOrEmpty(inputNombreJugador.text) ? "Almirante" : inputNombreJugador.text;

        // Reinicia el bot enemigo
        if (botEnemigo != null)
        {
            botEnemigo.GenerarFlotaEnemiga();
            botEnemigo.memoriaDisparosBot = new int[10, 10];
        }

        // Limpia el tablero visual
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                visualJugador[x, y].color = Color.white;
                visualEnemigo[x, y].color = Color.white;
            }
        }

        labelResultados.text = "Partida reiniciada. Nueva estrategia enemiga.";
        estadoActual = EstadoJuego.FaseColocacion;
        ActualizarMensajeEstado();
    }

    private string TraducirCoordenadas(int fila, int columna)
    {
        char letra = (char)('A' + columna); // Convierte el 0 en A, 1 en B, etc.
        int numero = fila + 1;              // Convierte el 0 en 1, 1 en 2, etc.
        return $"{letra}{numero}";
    }
    private void MostrarPantallaFinal(string mensaje)
    {
        estadoActual = EstadoJuego.FinJuego;
        labelEstado.text = "Partida finalizada";
        // Ocultamos los tableros y mostramos la pantalla de victoria
        //panelJuego.SetActive(false);
        panelFinJuego.SetActive(true);
        textoFinJuego.text = mensaje;
    }

    // Este método lo conectarás al botón "Jugar de Nuevo"
    public void ReiniciarJuego()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}