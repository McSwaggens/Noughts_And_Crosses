using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
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
        public Font font = new Font("Inconsolata", 120);


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
            GAME, WIN_LOS
        }

        

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
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
                }
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(Color.Black);
            GL.LoadIdentity();
            
            Random random = new Random();

            

            for (int x = 0; x < nodeWidth; x++)
            {
                for (int y = 0; y < nodeHeight; y++)
                {
                    int w = Width / nodeWidth;
                    int h = Height / nodeHeight;
                    Node node = nodeMap[x, y];
                    Color[] colors = node.getColors();
                    if (node.taken)
                    {
                        DrawSquare(x * w, y * h, (x * w) + w, (y * h) + h, colors[1], colors[0], colors[1], colors[0]);
                        textPrinter.Begin();
                        OpenTK.Graphics.TextExtents ex = textPrinter.Measure(" ", font);
                        GL.Translate((x * w) + ((w / 2) - (ex.BoundingBox.Width / 2)), ((y * h) + ((h / 2) - (ex.BoundingBox.Height / 2))), 0);
                        textPrinter.Print(node.team == Player.Human ? "X" : "O", font, Color.Black);
                        textPrinter.End();
                        GL.LoadIdentity();
                    }
                    else
                    if (node.hovered)
                        DrawSquare(x * w, y * h, (x * w) + w, (y * h) + h, Color.White, Color.WhiteSmoke, Color.White, Color.WhiteSmoke);
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

        public Color[] getColors() => team == Player.Human ? 
            new Color[] { Color.FromArgb(0, 0, 255), Color.FromArgb(70, 70, 255) } : 
            new Color[] { Color.FromArgb(255, 0, 0), Color.FromArgb(255, 70, 70) };

        public Node(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public enum Player
    {
        AI, Human
    }
}
