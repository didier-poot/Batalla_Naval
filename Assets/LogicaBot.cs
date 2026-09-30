using UnityEngine;
using System.Collections.Generic;

public class LogicaBot : MonoBehaviour
{
    // Matriz 10x10 que representa el tablero del bot. 
    // 0 = Agua, 1 al 5 = ID de Barco específico
    public int[,] tableroEnemigo = new int[10, 10];
    
    // Tamaños clásicos de los barcos de Batalla Naval
    private int[] tamanosBarcos = { 5, 4, 3, 3, 2 };

    // Diccionarios para guardar los nombres y la vida restante de cada barco
    public Dictionary<int, string> nombresBarcos = new Dictionary<int, string>();
    public Dictionary<int, int> vidaBarcos = new Dictionary<int, int>();

    void Start()
    {
        GenerarFlotaEnemiga();
    }

    public void GenerarFlotaEnemiga()
    {
        // 1. Inicializar la información de los barcos (ID, Nombre) y (ID, Vida)
        nombresBarcos.Clear();
        vidaBarcos.Clear();
        
        nombresBarcos.Add(1, "Portaaviones"); vidaBarcos.Add(1, 5); // ID 1 (Tamaño 5)
        nombresBarcos.Add(2, "Acorazado");    vidaBarcos.Add(2, 4); // ID 2 (Tamaño 4)
        nombresBarcos.Add(3, "Submarino");    vidaBarcos.Add(3, 3); // ID 3 (Tamaño 3)
        nombresBarcos.Add(4, "Crucero");      vidaBarcos.Add(4, 3); // ID 4 (Tamaño 3)
        nombresBarcos.Add(5, "Destructor");   vidaBarcos.Add(5, 2); // ID 5 (Tamaño 2)

        // 2. Limpiar el tablero por si se reinicia el juego
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                tableroEnemigo[x, y] = 0;
            }
        }

        // 3. Intentar colocar cada barco de la lista usando su ID único
        for (int i = 0; i < tamanosBarcos.Length; i++)
        {
            int tamano = tamanosBarcos[i];
            int barcoID = i + 1; // Los IDs serán 1, 2, 3, 4 y 5
            bool barcoColocado = false;

            while (!barcoColocado)
            {
                // Decidir rotación: true = horizontal, false = vertical
                bool esHorizontal = Random.Range(0, 2) == 0;
                
                // Elegir una coordenada inicial aleatoria (de 0 a 9)
                int fila = Random.Range(0, 10);
                int columna = Random.Range(0, 10);

                // Verificar si cabe y si el espacio está libre
                if (ValidarPosicionLibre(fila, columna, tamano, esHorizontal))
                {
                    // Registrar el barco en la matriz marcando con su ID ('barcoID')
                    for (int j = 0; j < tamano; j++)
                    {
                        if (esHorizontal)
                            tableroEnemigo[fila, columna + j] = barcoID;
                        else
                            tableroEnemigo[fila + j, columna] = barcoID;
                    }
                    barcoColocado = true; 
                }
            }
        }
        
        Debug.Log("La flota enemiga ha tomado posiciones estratégicas con IDs asignados.");
    }

    private bool ValidarPosicionLibre(int fila, int columna, int tamano, bool esHorizontal)
    {
        // Comprobación A: Límites del tablero
        // Si al sumar el tamaño del barco nos pasamos de 10, se sale del borde
        if (esHorizontal)
        {
            if (columna + tamano > 10) return false;
        }
        else
        {
            if (fila + tamano > 10) return false;
        }

        // Comprobación B: Colisiones con otros barcos ya colocados
        for (int i = 0; i < tamano; i++)
        {
            if (esHorizontal)
            {
                // Si la celda es distinta de 0, ya hay un barco ahí
                if (tableroEnemigo[fila, columna + i] != 0) return false; 
            }
            else
            {
                if (tableroEnemigo[fila + i, columna] != 0) return false; 
            }
        }

        // Si sobrevive a ambas comprobaciones, la posición es perfecta
        return true; 
    }

    // Matriz para recordar dónde ha disparado el bot (0 = no disparado, 1 = disparado)
    public int[,] memoriaDisparosBot = new int[10, 10];

    // Lista con casillas sospechosas: son vecinas a un disparo acertado
    private readonly List<Vector2Int> casillasSospechosas = new List<Vector2Int>();

    // Este método es llamado por la GUI para pedirle al bot sus coordenadas de ataque.
    public Vector2Int TurnoDeAtaqueBot()
    {
        Vector2Int coordenada;

        if (casillasSospechosas.Count > 0)
        {
            int indiceAleatorio = Random.Range(0, casillasSospechosas.Count);
            coordenada = casillasSospechosas[indiceAleatorio];
            casillasSospechosas.RemoveAt(indiceAleatorio);
        }
        else
        {
            int fila, columna;

            do
            {
                fila = Random.Range(0, 10);
                columna = Random.Range(0, 10);
            } while (memoriaDisparosBot[fila, columna] != 0);

            coordenada = new Vector2Int(fila, columna);
        }

        // Marcar la celda como disparada para no repetirla
        memoriaDisparosBot[coordenada.x, coordenada.y] = 1;

        return coordenada;
    }

    // Este método se puede invocar desde la GUI o desde otra clase cuando el bot recibe el resultado del disparo.
    public void RegistrarResultadoAtaque(int fila, int columna, bool acerto)
    {
        if (acerto)
        {
            AddCasillaSospechosa(fila, columna);
            AgregarVecinasSospechosas(fila, columna);
        }
    }

    private void AddCasillaSospechosa(int fila, int columna)
    {
        Vector2Int casilla = new Vector2Int(fila, columna);

        if (!casillasSospechosas.Contains(casilla) && memoriaDisparosBot[fila, columna] == 1)
        {
            casillasSospechosas.Add(casilla);
        }
    }

    private void AgregarVecinasSospechosas(int fila, int columna)
    {
        int[] desplazamientos = { -1, 0, 1 };

        foreach (int dx in desplazamientos)
        {
            foreach (int dy in desplazamientos)
            {
                if (Mathf.Abs(dx) == Mathf.Abs(dy))
                    continue;

                int nuevaFila = fila + dx;
                int nuevaColumna = columna + dy;

                if (EsCasillaValida(nuevaFila, nuevaColumna) && memoriaDisparosBot[nuevaFila, nuevaColumna] == 0)
                {
                    Vector2Int vecina = new Vector2Int(nuevaFila, nuevaColumna);

                    if (!casillasSospechosas.Contains(vecina))
                        casillasSospechosas.Add(vecina);
                }
            }
        }
    }

    private bool EsCasillaValida(int fila, int columna)
    {
        return fila >= 0 && fila < 10 && columna >= 0 && columna < 10;
    }
}