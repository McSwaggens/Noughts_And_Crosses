using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Noughts_And_Crosses
{
    public class NAC : GameWindow
    {
        //General
        public Node[,] nodeMap;
        public int nodeWidth;
        public int nodeHeight;
        public OpenTK.Graphics.TextPrinter textPrinter = new OpenTK.Graphics.TextPrinter(OpenTK.Graphics.TextQuality.High);
        public Font boxFont = new Font("8bit", 120);
        public Font titleFont = new Font("8bit", 40);
        private Random random = new Random();
        private Player WINNER = Player.AI;
        private const int WIN_SCREEN_TICK_MAX = (60) * 2;
        private int WIN_SCREEN_TICK_CURRENT = WIN_SCREEN_TICK_MAX;
        private int AI_WINS = 0;
        private int HU_WINS = 0;
        private Screen ScreenState = Screen.GAME;
        private Node[] FlashNodes;

        //Mouse
        public bool isMouseDown = false;
        public bool safeSwitch = false;

        public NAC(int Width, int Height, int Division) : base (Width, Height)
        {
            Title = "Naughts and Crosses";
            nodeMap = new Node[Division, Division];
            nodeWidth = nodeMap.GetLength(0);
            nodeHeight = nodeMap.GetLength(1);
            for (int x = 0; x < nodeWidth; x++)
                for (int y = 0; y < nodeHeight; y++) nodeMap[x, y] = new Node(x, y);
        }

        protected override void OnLoad(EventArgs e)
        {
            Mouse.ButtonDown += MouseDown;
            Mouse.ButtonUp += MouseUp;
        }

        private void MouseUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            safeSwitch = false;
        }

        private void MouseDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(ClientRectangle);
            GL.MatrixMode(MatrixMode.Projection);
            //GL.LoadIdentity();
            GL.Ortho(0, ClientRectangle.Width, ClientRectangle.Height, 0, -1, 0);

        }

        enum Screen
        {
            GAME, FLASH
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (ScreenState == Screen.FLASH)
            {
                foreach (Node n in FlashNodes)
                {
                    n.Enabed = !n.Enabed;
                }
                WIN_SCREEN_TICK_CURRENT--;
                if (WIN_SCREEN_TICK_CURRENT == 0)
                {
                    ScreenState = Screen.GAME;
                    foreach (Node n in FlashNodes)
                    {
                        n.Enabed = true;
                    }
                    ResetBoard();
                    WIN_SCREEN_TICK_CURRENT = WIN_SCREEN_TICK_MAX;
                }
                return;
            }
            int X = Mouse.X;
            int Y = Mouse.Y;
            Node node = null;
            int w = (Width / nodeWidth);
            int h = (Height / nodeHeight);
            foreach (Node n in nodeMap)
                if (
                    X > (n.X * w) && X < ((n.X+1) * w)
                    && Y > (n.Y) * h && Y < ((n.Y+1) * h)
                    ) node = n;
                else n.hovered = false;

            if (node != null)
            {
                node.hovered = true;
                if (!node.taken && isMouseDown && !safeSwitch)
                {
                    safeSwitch = true;
                    node.taken = true;
                    node.team = Player.Human;
                    bool cleared = CheckBoard();
                    if (!cleared)
                        AI_Calculate_Turn();
                }
            }
        }

        void AI_Calculate_Turn()
        {
            Node[,] map = nodeMap;
            List<Node> WinNodes = new List<Node>();
            List<Node> MyNodes = new List<Node>();
            List<Node> SaveNodes = new List<Node>();
            List<Node> FreeNodes = new List<Node>();

            Node[,] nodeMap_DIAG = GenerateDiag();
            int DIAG_WIDTH = nodeWidth + 2;

            for (int x = 0; x < DIAG_WIDTH; x++)
            {
                int lineNodeCount_Enemy = 0;
                int lineNodeCount_AI = 0;
                List<Node> TakenNodes = new List<Node>();
                List<Node> __FreeNodes = new List<Node>();
                for (int y = 0; y < nodeHeight; y++)
                {
                    Node node = nodeMap_DIAG[x, y];
                    if (node.taken)
                    {
                        if (node.team == Player.Human) { lineNodeCount_Enemy++; } else { lineNodeCount_AI++; MyNodes.Add(node); }
                        TakenNodes.Add(node);
                    }
                    else __FreeNodes.Add(node);
                }
                if (lineNodeCount_AI + lineNodeCount_Enemy > 0)
                {
                    if (lineNodeCount_Enemy == nodeWidth-1 && lineNodeCount_AI == 0) SaveNodes.Add(__FreeNodes[0]);
                    if (lineNodeCount_AI == nodeWidth-1 && lineNodeCount_Enemy == 0) WinNodes.Add(__FreeNodes[0]);
                    if (lineNodeCount_AI == 1 && lineNodeCount_Enemy == 1) { /*STALL*/ }
                }
                FreeNodes.AddRange(__FreeNodes);
            }


            for (int y = 0; y < nodeHeight; y++)
            {
                int lineNodeCount_Enemy = 0;
                int lineNodeCount_AI = 0;
                List<Node> TakenNodes = new List<Node>();
                List<Node> __FreeNodes = new List<Node>();
                for (int x = 0; x < nodeWidth; x++)
                {
                    Node node = map[x, y];
                    if (node.taken)
                    {
                        if (node.team == Player.Human) { lineNodeCount_Enemy++; } else { lineNodeCount_AI++; MyNodes.Add(node); }
                        TakenNodes.Add(node);
                    }
                    else __FreeNodes.Add(node);
                }
                if (lineNodeCount_AI + lineNodeCount_Enemy > 0)
                {
                    if (lineNodeCount_Enemy == nodeHeight-1 && lineNodeCount_AI == 0) SaveNodes.Add(__FreeNodes[0]);
                    if (lineNodeCount_AI == nodeHeight-1 && lineNodeCount_Enemy == 0) WinNodes.Add(__FreeNodes[0]);
                    if (lineNodeCount_AI == 1 && lineNodeCount_Enemy == 1) { /*STALL*/ }
                }
                FreeNodes.AddRange(__FreeNodes);
            }


            if (WinNodes.Count > 0)
            {
                WinNodes[0].Take(Player.AI);
            }
            else
            if (SaveNodes.Count > 0)
            {
                SaveNodes[0].Take(Player.AI);
            }
            else if (FreeNodes.Count > 0)
            {
                FreeNodes[random.Next(0, FreeNodes.Count)].Take(Player.AI);
            }

            CheckBoard();
        }

        bool CheckBoard()
        {
            int Total_Count = 0;
            //X
            for (int y = 0; y < nodeHeight; y++) {
                int N_AI = 0;
                int N_PL = 0;
                List<Node> nodes = new List<Node>();
                for (int x = 0; x < nodeWidth; x++)
                {
                    Node node = nodeMap[x, y];
                    if (node.taken)
                    {
                        if (node.team == Player.AI) N_AI++; else N_PL++;
                        Total_Count++;
                        nodes.Add(node);
                    }
                }
                if (N_PL == nodeWidth)
                {
                    HU_WINS++;
                    Flash(nodes.ToArray());
                    return true;
                    //ResetBoard();
                }
                else if (N_AI == nodeWidth)
                {
                    AI_WINS++;
                    Flash(nodes.ToArray());
                    return true;
                    //ResetBoard();
                }
            }

            Node[,] nodemap_DIAG = GenerateDiag();

            int DIAG_WIDTH = nodeWidth + 2;

            //Y
            for (int x = 0; x < DIAG_WIDTH; x++)
            {
                int N_AI = 0;
                int N_PL = 0;
                List<Node> nodes = new List<Node>();
                for (int y = 0; y < nodeHeight; y++)
                {
                    Node node = nodemap_DIAG[x, y];
                    if (node.taken)
                    {
                        if (node.team == Player.AI) N_AI++; else N_PL++;
                        nodes.Add(node);
                    }
                }
                if (N_PL == nodeHeight)
                {
                    HU_WINS++;
                    Flash(nodes.ToArray());
                    return true;
                    //ResetBoard();
                }
                else if (N_AI == nodeHeight)
                {
                    AI_WINS++;
                    Flash(nodes.ToArray());
                    return true;
                    //ResetBoard();
                }
            }

            if (Total_Count == nodeWidth * nodeHeight)
            {
                ResetBoard();
                return true;
            }
            return false;
        }

        void Flash(Node[] nodes)
        {
            FlashNodes = nodes;
            ScreenState = Screen.FLASH;
        }

        Node[,] GenerateDiag()
        {
            Node[,] nodemap_DIAG = new Node[nodeWidth + 2, nodeHeight];
            foreach (Node node in nodeMap) nodemap_DIAG[node.X, node.Y] = node;
            for (int x = 0, y = 0; x < nodeWidth && y < nodeHeight; x++, y++)
            {
                nodemap_DIAG[nodeWidth, y] = nodeMap[x, y];
            }
            for (int x = nodeWidth - 1, y = 0; x > -1 && y < nodeHeight; x--, y++)
            {
                nodemap_DIAG[nodeWidth + 1, y] = nodeMap[x, y];
            }
            return nodemap_DIAG;
        }

        void ResetBoard()
        {
            nodeMap = new Node[nodeWidth, nodeHeight];
            for (int x = 0; x < nodeWidth; x++)
                for (int y = 0; y < nodeHeight; y++) nodeMap[x, y] = new Node(x, y);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(Color.Black);
            GL.LoadIdentity();
            
            for (int x = 0; x < nodeWidth; x++)
            {
                for (int y = 0; y < nodeHeight; y++)
                {
                    int w = Width / nodeWidth;
                    int h = Height / nodeHeight;
                    Node node = nodeMap[x, y];
                    Color[] colors = node.getColors();
                    if (node.Enabed)
                    {
                        if (node.taken)
                        {
                            DrawSquare(x * w, y * h, (x * w) + w, (y * h) + h, colors[1], colors[0], colors[1], colors[0]);
                            textPrinter.Begin();
                            OpenTK.Graphics.TextExtents ex = textPrinter.Measure(" ", boxFont);
                            GL.Translate((x * w) + ((w / 2) - (ex.BoundingBox.Width / 2)), ((y * h) + ((h / 2) - (ex.BoundingBox.Height / 2))), 0);
                            textPrinter.Print(node.team == Player.Human ? "X" : "O", boxFont, Color.Black);
                            textPrinter.End();
                            GL.LoadIdentity();
                        }
                        else
                        if (node.hovered)
                            DrawSquare(x * w, y * h, (x * w) + w, (y * h) + h, Color.White, Color.WhiteSmoke, Color.White, Color.WhiteSmoke);
                    }
                }
            }

            GL.Color3(Color.White);

            for (int w = 1; w < nodeMap.GetLength(0); w++)
            {
                GL.Begin(BeginMode.Lines);
                GL.Vertex2(Orth((Width / nodeWidth) * w, 0));
                GL.Vertex2(Orth((Width / nodeWidth) * w, Height));
                GL.End();
            }
            for (int y = 1; y < nodeMap.GetLength(1); y++)
            {
                GL.Begin(BeginMode.Lines);
                GL.Vertex2(Orth(0, (Height / nodeHeight) * y));
                GL.Vertex2(Orth(Width, (Height / nodeHeight) * y));
                GL.End();
            }

            textPrinter.Begin();
            GL.Translate(2, 2, 0);
            textPrinter.Print($"Human: {HU_WINS}     AI: {AI_WINS}", titleFont, Color.Gray);

            textPrinter.End();

            SwapBuffers();
        }

        void DrawSquare(int sx, int sy, int ex, int ey)
        {
            GL.Begin(BeginMode.Quads);
            GL.Vertex2(Orth(sx, sy));
            GL.Vertex2(Orth(ex, sy));
            GL.Vertex2(Orth(ex, ey));
            GL.Vertex2(Orth(sx, ey));
            GL.End();
        }

        void DrawSquare(int sx, int sy, int ex, int ey, Color c1, Color c2, Color c3, Color c4)
        {
            GL.Begin(BeginMode.Quads);
            GL.Color3(c1);
            GL.Vertex2(Orth(sx, sy));
            GL.Color3(c2);
            GL.Vertex2(Orth(ex, sy));
            GL.Color3(c3);
            GL.Vertex2(Orth(ex, ey));
            GL.Color3(c4);
            GL.Vertex2(Orth(sx, ey));
            GL.End();
        }


        public Vector2 Orth(int x, int y)
        {
            Vector2 vector = new Vector2();
            vector.X = (float)(x * 2.0 / Width - 1.0);
            vector.Y = (float)(y * -2.0 / Height + 1.0);
            return vector;
        }
    }


    public class Node
    {
        private static Random random = new Random();

        public int X, Y;
        public bool hovered = false;
        public bool taken = false;
        public Player team;
        public bool Enabed = true;

        public Color[] getColors() => team == Player.Human ? 
            new Color[] { Color.FromArgb(0, 0, 255), Color.FromArgb(70, 70, 255) } : 
            new Color[] { Color.FromArgb(255, 0, 0), Color.FromArgb(255, 70, 70) };

        public Node(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void Take(Player player)
        {
            taken = true;
            hovered = false;
            team = player;
        }
    }

    public enum Player
    {
        AI, Human
    }
}
