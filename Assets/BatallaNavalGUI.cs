using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BatallaNavalGUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TMP_Text labelEstado;
    public GameObject botonPrefab;
    public Transform gridParent;

    // 1. MÁQUINA DE ESTADOS (Enum)
    public enum EstadoJuego { FaseColocacion, TurnoJugador, TurnoEnemigo, FinJuego }
    private EstadoJuego estadoActual;

    // 2. LA CUADRÍCULA LÓGICA (Back-end)
    // 0 = Agua, 1 = Barco, 2 = Impacto (Fuego), 3 = Fallo (Agua salpicada)
    private int[,] tableroLogico = new int[10, 10];
    
    // Matriz para guardar la referencia visual de los botones
    private Image[,] tableroVisual = new Image[10, 10];

    private int barcosColocados = 0;
    private const int MAX_BARCOS = 5;

    void Start()
    {
        GenerarTablero();
        CambiarEstado(EstadoJuego.FaseColocacion);
    }

    void GenerarTablero()
    {
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                // Inicializar la lógica (todo es agua al principio)
                tableroLogico[x, y] = 0;

                // Crear el botón visual
                GameObject nuevoBoton = Instantiate(botonPrefab, gridParent);
                nuevoBoton.name = $"Casilla_{x}_{y}";
                
                // Guardar la referencia visual (su componente Image)
                tableroVisual[x, y] = nuevoBoton.GetComponent<Image>();

                // Capturar coordenadas para el evento de clic
                int posX = x;
                int posY = y;
                
                // Añadir la función de clic separada de la UI
                nuevoBoton.GetComponent<Button>().onClick.AddListener(() => AlHacerClicEnCasilla(posX, posY));
            }
        }
    }

    // 3. CONTROLADOR CENTRAL DE EVENTOS
    public void AlHacerClicEnCasilla(int x, int y)
    {
        // El comportamiento del clic depende enteramente del Estado actual
        switch (estadoActual)
        {
            case EstadoJuego.FaseColocacion:
                ColocarBarco(x, y);
                break;
                
            case EstadoJuego.TurnoJugador:
                AtacarCasilla(x, y);
                break;
                
            case EstadoJuego.TurnoEnemigo:
            case EstadoJuego.FinJuego:
                // No hacer nada si no es el turno del jugador
                Debug.Log("No puedes interactuar en este momento.");
                break;
        }
    }

    // Lógica pura: Colocar barco
    private void ColocarBarco(int x, int y)
    {
        if (tableroLogico[x, y] == 0 && barcosColocados < MAX_BARCOS)
        {
            tableroLogico[x, y] = 1; // 1 = Barco
            barcosColocados++;
            
            ActualizarVisualCasilla(x, y);
            labelEstado.text = $"Coloca tus barcos ({barcosColocados}/{MAX_BARCOS})";

            if (barcosColocados >= MAX_BARCOS)
            {
                CambiarEstado(EstadoJuego.TurnoJugador);
            }
        }
    }

    // Lógica pura: Atacar
    private void AtacarCasilla(int x, int y)
    {
        // Solo podemos atacar agua o barcos, no lugares ya atacados (2 o 3)
        if (tableroLogico[x, y] == 0)
        {
            tableroLogico[x, y] = 3; // 3 = Fallo
            ActualizarVisualCasilla(x, y);
            CambiarEstado(EstadoJuego.TurnoEnemigo);
        }
        else if (tableroLogico[x, y] == 1)
        {
            tableroLogico[x, y] = 2; // 2 = Impacto
            ActualizarVisualCasilla(x, y);
            CambiarEstado(EstadoJuego.TurnoEnemigo);
        }
    }

    // Lógica pura: El Enemigo ataca (Simulado por ahora)
    private IEnumerator SimularTurnoEnemigo()
    {
        yield return new WaitForSeconds(1.5f);
        
        // Aquí conectaremos la Base de Datos o la IA más adelante
        labelEstado.text = "El enemigo falló su tiro.";
        
        yield return new WaitForSeconds(1f);
        CambiarEstado(EstadoJuego.TurnoJugador);
    }

    // 4. ACTUALIZACIÓN VISUAL (Aislada de la lógica matemática)
    private void ActualizarVisualCasilla(int x, int y)
    {
        int estadoLogico = tableroLogico[x, y];
        Image imagenBoton = tableroVisual[x, y];

        // Por ahora cambiamos el color, luego cambiaremos el sprite (la imagen)
        switch (estadoLogico)
        {
            case 0: // Agua
                imagenBoton.color = Color.white; 
                break;
            case 1: // Barco
                imagenBoton.color = Color.gray; 
                break;
            case 2: // Impacto
                imagenBoton.color = Color.red; 
                break;
            case 3: // Fallo
                imagenBoton.color = Color.cyan; 
                break;
        }
    }

    // Gestor de transiciones de estado
    private void CambiarEstado(EstadoJuego nuevoEstado)
    {
        estadoActual = nuevoEstado;

        switch (estadoActual)
        {
            case EstadoJuego.FaseColocacion:
                labelEstado.text = $"Coloca tus barcos (0/{MAX_BARCOS})";
                break;
            case EstadoJuego.TurnoJugador:
                labelEstado.text = "¡Ataca al enemigo!";
                break;
            case EstadoJuego.TurnoEnemigo:
                labelEstado.text = "Turno del enemigo...";
                StartCoroutine(SimularTurnoEnemigo());
                break;
        }
    }
}