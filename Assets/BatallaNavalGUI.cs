using UnityEngine;
using UnityEngine.UI;
using TMPro; // Necesario para TextMeshPro
using System.Collections;
using System.Collections.Generic;

public class BatallaNavalGUI : MonoBehaviour
{
    public enum FaseJuego { Colocar, Jugar }
    private FaseJuego fase = FaseJuego.Colocar;

    // Tamaño del tablero de 10x10 según el código original
    private const int TAMANO = 10; 

    [Header("Referencias UI")]
    public TextMeshProUGUI labelEstado;
    public GameObject botonPrefab; 
    public Transform gridParent; // Asignar un panel con GridLayoutGroup

    private Button[,] botones;
    private List<Vector2Int> barcosJugador = new List<Vector2Int>();
    private List<Vector2Int> barcosEnemigo = new List<Vector2Int>();

    void Start()
    {
        botones = new Button[TAMANO, TAMANO];
        labelEstado.text = "Coloca tus barcos (0/5)";
        
        // Simulación de la función crear_barcos(TAMANO) importada en tu script
        GenerarBarcosEnemigo(); 
        
        GenerarTablero();
    }

    void GenerarTablero()
    {
        for (int i = 0; i < TAMANO; i++)
        {
            for (int j = 0; j < TAMANO; j++)
            {
                // Capturar variables locales para el delegado onClick
                int fila = i;
                int col = j;
                
                GameObject nuevoBotonObj = Instantiate(botonPrefab, gridParent);
                Button boton = nuevoBotonObj.GetComponent<Button>();
                
                // Enlace equivalente a lambda i=i, j=j: disparo(i, j)
                boton.onClick.AddListener(() => Disparo(fila, col));
                
                botones[i, j] = boton;
            }
        }
    }

    void Disparo(int fila, int col)
    {
        Vector2Int posicion = new Vector2Int(fila, col);

        if (fase == FaseJuego.Colocar)
        {
            // Límite de 5 barcos
            if (barcosJugador.Count >= 5 || barcosJugador.Contains(posicion)) return;

            botones[fila, col].GetComponent<Image>().color = Color.blue;
            barcosJugador.Add(posicion);
            labelEstado.text = $"Coloca tus barcos ({barcosJugador.Count}/5)";

            if (barcosJugador.Count == 5)
            {
                fase = FaseJuego.Jugar;
                labelEstado.text = "¡Listo! Haz clic en el tablero para atacar.";
            }
        }
        else if (fase == FaseJuego.Jugar)
        {
            TextMeshProUGUI textoBoton = botones[fila, col].GetComponentInChildren<TextMeshProUGUI>();
            if (textoBoton.text != "") return;

            labelEstado.text = "Consultando coordenadas en la Base de Datos...";
            BloquearTablero(false); // Deshabilitar botones libres temporalmente
            
            // Reemplazo del threading.Thread
            StartCoroutine(ConsultarBDYDisparar(fila, col));
        }
    }

    // Equivalente a def consultar_bd_y_disparar()
    IEnumerator ConsultarBDYDisparar(int fila, int col)
    {
        // Reemplazo de time.sleep(1.0)
        yield return new WaitForSeconds(1.0f); 
        
        bool acierto = barcosEnemigo.Contains(new Vector2Int(fila, col));
        ActualizarGUIDisparo(fila, col, acierto);
    }

    // Equivalente a def actualizar_gui_disparo()
    void ActualizarGUIDisparo(int fila, int col, bool acierto)
    {
        Button boton = botones[fila, col];
        TextMeshProUGUI textoBoton = boton.GetComponentInChildren<TextMeshProUGUI>();
        Image imagenBoton = boton.GetComponent<Image>();

        if (acierto)
        {
            textoBoton.text = "X"; // Usar 'X' en lugar del emoji para fuentes estándar
            imagenBoton.color = Color.red;
            labelEstado.text = "¡Impacto confirmado!";
        }
        else
        {
            textoBoton.text = "O";
            // Color 'lightblue' de Tkinter equivale a cyan en Unity básico
            imagenBoton.color = Color.cyan; 
            labelEstado.text = "Agua... Turno del enemigo.";
        }

        boton.interactable = false;
        BloquearTablero(true); // Rehabilitar casillas vacías
    }

    void BloquearTablero(bool interactuable)
    {
        for (int i = 0; i < TAMANO; i++)
        {
            for (int j = 0; j < TAMANO; j++)
            {
                TextMeshProUGUI textoBoton = botones[i, j].GetComponentInChildren<TextMeshProUGUI>();
                // Solo modificar estado normal si no se ha clickeado aún
                if (textoBoton.text == "")
                {
                    botones[i, j].interactable = interactuable;
                }
            }
        }
    }

    void GenerarBarcosEnemigo()
    {
        // Función placeholder para sustituir la lógica de juego.py
        barcosEnemigo.Add(new Vector2Int(0, 0));
        barcosEnemigo.Add(new Vector2Int(1, 1));
        barcosEnemigo.Add(new Vector2Int(2, 2));
        barcosEnemigo.Add(new Vector2Int(3, 3));
        barcosEnemigo.Add(new Vector2Int(4, 4));
    }
}