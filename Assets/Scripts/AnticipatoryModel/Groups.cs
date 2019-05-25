using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnticipatoryModel
{
    public static class Groups
    {
        // Group detection: predefined thresholds 
        const int MIN_DISTANCE = 2;             // Distancia Minima para comportamiento de grupos
        const int MAX_DISTANCE = 1000;         // Distancia Maxima para comportamiento de grupos
        const int MIN_ANGLE = 180;
        const int MAX_ANGLE = -180;

        // Deteccion de puntos extremos tangentes
        static void GetTangents(List<int> group, Dictionary<int, float> ttc, 
                                        Vector2 position, float radius,
                                        out Vector2 closestAgentPosition, 
                                        out Vector2 closestAgentVelocity,
                                        out List<Point> tangentsPoints,
                                        out List<Tuple<Point, Point>> points)
        {
            float minimoTTC = Mathf.Infinity, minDst = MAX_DISTANCE;
            Point agentPos = new Point(position);
            Point outer1_p1, outer1_p2, outer2_p1, outer2_p2;

            tangentsPoints = new List<Point>();
            points = new List<Tuple<Point, Point>>();

            closestAgentPosition = Vector2.zero;
            closestAgentVelocity = Vector2.zero;

            foreach (int neighborID in group)
            {
                //if (group.Contains(id)) break;
                if (!ttc.ContainsKey(neighborID)) continue;

                // Calculamos el agente mas cercano al observador
                AMAgent agent_tmp = Engine.Instance.GetAgent(neighborID);
                float dst = Vector2.Distance(position, agent_tmp.position);

                // Si este agente esta mas cerca de lo permitido entonces
                // no es tomado en cuenta
                if (dst < MIN_DISTANCE) { continue; }

                if (dst < minDst)
                {
                    closestAgentPosition = agent_tmp.position;
                    minDst = dst;
                }

                if (ttc[neighborID] < minimoTTC)
                {
                    closestAgentVelocity = agent_tmp.velocity;
                    minimoTTC = ttc[neighborID];
                }

                // Find tangents points from this agent to actual iteration neighbour
                Global.FindCircleCircleTangents(agentPos, radius,
                    new Point(agent_tmp.position), agent_tmp.radius,
                    out outer1_p1, out outer1_p2, out outer2_p1, out outer2_p2);

                // agregamos el vector tangente en direccion del agente observador
                dst = Point.Distance(outer1_p1, agentPos);
                float dst_2 = Point.Distance(outer1_p2, agentPos);

                if (dst < dst_2)
                {
                    tangentsPoints.Add(outer1_p2);
                    points.Add(new Tuple<Point, Point>(outer1_p1, outer1_p2));
                }
                else
                {
                    tangentsPoints.Add(outer1_p1);
                    points.Add(new Tuple<Point, Point>(outer1_p2, outer1_p1));
                }

                // agregamos el vector tangente en direccion del agente observador
                dst = Point.Distance(outer2_p1, agentPos);
                dst_2 = Point.Distance(outer2_p2, agentPos);
                if (dst < dst_2)
                {
                    tangentsPoints.Add(outer2_p2);
                    points.Add(new Tuple<Point, Point>(outer2_p1, outer2_p2));
                }
                else
                {
                    tangentsPoints.Add(outer2_p1);
                    points.Add(new Tuple<Point, Point>(outer2_p2, outer2_p1));
                }
            }
        }

        // Hallar tangentes externas
        static void GetExtremeTangents(Vector2 dir, Point agentPos, 
            List<Point> tangentsPoints, List<Tuple<Point, Point>> points, bool debug,
            out Point fl1, out Point fl2, out Point fr1, out Point fr2)
        {
            float minAngle = MIN_ANGLE, maxAngle = MAX_ANGLE, f_t1;
            Vector2 v_t1;
            Vector3 v3_t1, v3_t2;

            // Puntos para representar lineas tangentes extremas
            fl1 = new Point();
            fl2 = new Point();
            fr1 = new Point();
            fr2 = new Point();

            for (int i = 0; i < tangentsPoints.Count; i++)
            {
                // Debug: pintar extremos tangentes a cada agente
                if (debug)
                {
                    v3_t1 = new Vector3(points[i].Item1.x, 1.5f, points[i].Item1.y);
                    v3_t2 = new Vector3(points[i].Item2.x, 1.5f, points[i].Item2.y);
                    Debug.DrawLine(v3_t1, v3_t2, Color.blue);
                }

                // Vector que va desde este agente hasta punto tangente
                v_t1 = Point.MakeVector(agentPos, tangentsPoints[i]);

                // Angulo entre direccion del agente y punto tangente ending
                f_t1 = Vector2.SignedAngle(dir, v_t1);

                if (f_t1 <= minAngle)
                {
                    minAngle = f_t1;
                    fl1 = points[i].Item1;
                    fl2 = points[i].Item2;
                }

                if (f_t1 >= maxAngle)
                {
                    maxAngle = f_t1;
                    fr1 = points[i].Item1;
                    fr2 = points[i].Item2;
                }
            }
        }

        // Queremos hallar el radio,
        // Eso es la distancia que hay desde el punto ending al punto de corte de
        // una recta perpendicular con una de las tangentes y que pasa por
        // el punto ending con dicha recta tangente.

        // Para hallar el punto ending primero necesitamos el punto start de la bisectriz
        // Hallemos el punto de corte de las tangentes xd
        static Vector2 GetExtremeTangentsIntersectionPoint
            (Point fl1, Point fl2, Point fr1, Point fr2, out Line tangent1)
        {
            float f_t1, f_t2;
            Global.FindEqLine(fl1, fl2, out f_t1, out f_t2);
            tangent1 = new Line(f_t1, f_t2);                        // Linea Tangente 1

            Global.FindEqLine(fr1, fr2, out f_t1, out f_t2);
            Line tangent2 = new Line(f_t1, f_t2);                        // Linea Tangente 2

            // Punto de corte de ambas tangentes, llamado I
            return Line.IntersetionPoint(tangent1, tangent2);
        }

        public static bool PercivingGroups(Vector2 pov, Vector2 vel, float rad,
            List<int>group, Dictionary<int, float> ttc, out Vector2 gPos, out Vector2 gVel, out float gRad, bool debug)
        {
            gPos = Vector2.zero;
            gVel = Vector2.zero;
            gRad = 0;

            Point agentPos = new Point(pov);
            List<Point> tangentsPoints;
            List<Tuple<Point, Point>> points;
            Vector2 closestAgentPosition;

            GetTangents(group, ttc, pov, rad, out closestAgentPosition, out gVel, out tangentsPoints, out points);

            // Necesitamos al menos 2 agentes para formar un grupo
            if (tangentsPoints.Count < 4) return false;

            // Puntos para representar lineas tangentes extremas
            Point fl1, fl2, fr1, fr2;

            GetExtremeTangents(vel, agentPos, tangentsPoints, points, debug, out fl1, out fl2, out fr1, out fr2);

            // Vectores extremos tangentes, su suma es la bisectriz
            // v_t2: farLeft = fl1 - fl2;
            // v_t3: farRight = fr1 - fr2;
            Vector2 v_t2 = Point.MakeVector(fl1, fl2);
            Vector2 v_t3 = Point.MakeVector(fr1, fr2);
            Vector2 BI = v_t2.normalized + v_t3.normalized;

            if (Vector2.Angle(v_t2, v_t3) < 5 ||
                Vector2.Angle(v_t2, v_t3) > 90) return false;

            Line tangent1;
            Vector2 I = GetExtremeTangentsIntersectionPoint(fl1, fl2, fr1, fr2, out tangent1);

            // Magnitud de la bizectriz es distancia desde I 
            // closestAgentPosition previamente calculado
            float k = Vector2.Distance(I, closestAgentPosition);
            BI = BI.normalized * k;

            // La posicion del grupo viene dada por:
            // BI = gPos - I
            // gPos = bizectriz + I 
            gPos = BI + I;

            // Usando gPos y una de las rectas tangentes externas
            // se calcula una recta perpendicular a la recta 
            // tangente externa seleccionada

            // primero calculamos la pendiente de esta recta
            float f_t1 = -1 / tangent1.m;
            // y luego se construye la recta perpendicular
            float f_t2 = gPos.y - f_t1 * gPos.x;
            Line perpendicular = new Line(f_t1, f_t2);

            // Anja ahora finalmente podemos intersectar las rectas tangente 1 y esta xd
            Vector2 v_t1 = Line.IntersetionPoint(tangent1, perpendicular);

            // al fin tenemos el vendito punto de interseccion jajajajja
            // ahora la distancia desde el al centro es el radio xD
            // radio del agente virual
            gRad = Vector2.Distance(gPos, v_t1);

            if (debug)
            {
                DrawCircle(0.1f, I);
                Debug.DrawRay(new Vector3(I.x, 1.5f, I.y), new Vector3(v_t2.x, 0, v_t2.y), Color.gray);
                Debug.DrawRay(new Vector3(I.x, 1.5f, I.y), new Vector3(v_t3.x, 0, v_t3.y), Color.gray);
                Debug.DrawRay(new Vector3(I.x, 1.5f, I.y), new Vector3(BI.x, 0, BI.y), Color.green);

                // Another way to paintarlo
                //Debug.DrawLine(new Vector3(I.x, 1.5f, I.y), new Vector3(fl2.x, 1.5f, fl2.y));
                //Debug.DrawLine(new Vector3(I.x, 1.5f, I.y), new Vector3(fr2.x, 1.5f, fr2.y));

                // Posicion del grupo
                DrawCircle(0.1f, gPos);

                // Radio del grupo
                DrawCircle(gRad, gPos);
            }

            return true;
        }

        static void DrawCircle(float radius, Vector2 position) {
            float f_t4 = 0;
            float f_t2 = radius * Mathf.Cos(f_t4);
            float f_t3 = radius * Mathf.Sin(f_t4);
            Vector3 v3_t1 = new Vector3(position.x + f_t2, 1.5f, position.y + f_t3);
            Vector3 v3_t2 = v3_t1;
            Vector3 v3_t3 = v3_t1;

            for (f_t4 = 0.1f; f_t4 < Mathf.PI * 2; f_t4 += 0.1f)
            {
                f_t2 = radius * Mathf.Cos(f_t4);
                f_t3 = radius * Mathf.Sin(f_t4);

                v3_t2 = new Vector3(position.x + f_t2, 1.5f, position.y + f_t3);
                Debug.DrawLine(v3_t1, v3_t2);
                v3_t1 = v3_t2;
            }
            Debug.DrawLine(v3_t1, v3_t3);
        }
    }
}
