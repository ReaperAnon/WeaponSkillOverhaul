using System;
using System.Collections.Generic;

namespace BuchheimTree
{
    /// <summary>
    /// Tree node for generating Buchheim tree distributions. Set the data member for sorting purposes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Node<T>
    {
        public T? Data { get; set; }
        public Node<T>? Parent { get; set; } = null; // Actual parent of the node.
        public Node<T>? Ancestor { get; set; } = null;
        public List<Node<T>>? Children { get; set; } = null;
        public Node<T>? Thread { get; set; } = null; // Pointer to a successor that has the same contour, added when two subtrees of different heights are combined.

        public double Offset { get; set; } = 0;
        public double PreliminaryPos { get; set; } = 0;
        public double Number { get; set; } = 0;
        public double Change { get; set; } = 0;
        public double Shift { get; set; } = 0;

        public double Pos { get; set; } = 0;
        public double Level { get; set; } = 0;

        public bool IsRoot()
        {
            return Parent is null;
        }

        public int GetChildCount()
        {
            return Children?.Count ?? 0;
        }

        public Node<T>? GetPrevSibling()
        {
            if (IsRoot())
                return null;

            int prevIdx = Parent!.Children!.IndexOf(this) - 1;
            return prevIdx < 0 ? null : Parent!.Children![prevIdx];
        }

        public Node<T>? GetFirstSibling()
        {
            return IsRoot() ? null : Parent!.Children![0];
        }

        public Node<T>? GetFirstChild()
        {
            return GetChildCount() > 0 ? Children![0] : null;
        }

        public Node<T>? GetLastChild()
        {
            return GetChildCount() > 0 ? Children![^1] : null;
        }
    }

    /// <summary>
    /// Class to run tree generation with. Run GenerateTree with a tree constructed out of Node<T> types.
    /// </summary>
    public static class TreeBuilder
    {
        private static double Distance { get; set; } = 1;

        public static void GenerateTree<T>(ref Node<T> tree, double spacing)
        {
            Distance = spacing;
            Run(tree);
        }

        static int GetNodeLevel<T>(Node<T> node)
        {
            var level = 0;
            var parent = node.Parent;
            while (parent is not null)
            {
                ++level;
                parent = parent.Parent;
            }

            return level;
        }

        static void Run<T>(Node<T> root)
        {
            if (root.Children is null)
                throw new Exception("The children array of the tree's root is null.\n\n");

            if (root.Children.Count <= 0)
                throw new Exception("The children array of the tree's root is empty.\n\n");

            foreach (var node in root.Children!)
            {
                if (node is null)
                    continue;

                MakeTreeLayout(node, root);
            }

            FirstWalk(root, 0);
            SecondWalk(root, -root.PreliminaryPos);
        }

        static void MakeTreeLayout<T>(Node<T> node, Node<T> parent)
        {
            node.Offset = 0;
            node.Thread = null;
            node.Parent = parent;
            node.Ancestor = node;
            for (int i = 0; i < node.GetChildCount(); ++i)
                MakeTreeLayout(node.Children![i], node);
        }

        static private void SecondWalk<T>(Node<T> node, double offset)
        {
            node.Level = GetNodeLevel(node);
            node.Pos = node.PreliminaryPos + offset;
            for (int i = 0; i < node.GetChildCount(); ++i)
                SecondWalk(node.Children![i], offset + node.Offset);
        }

        static private void FirstWalk<T>(Node<T> node, int num)
        {
            node.Number = num;

            if (node.GetChildCount() == 0)
            {
                Node<T>? prevSibling = node.GetPrevSibling();
                node.PreliminaryPos = prevSibling is null ? 0 : prevSibling.PreliminaryPos + Distance;
            }
            else
            {
                Node<T> defaultAncestor = node.GetFirstChild()!;
                for (int i = 0; i < node.GetChildCount(); ++i)
                {
                    FirstWalk(node.Children![i], i);
                    defaultAncestor = Apportion(node.Children![i], defaultAncestor);
                }

                ExecuteShifts(node);

                Node<T> firstChild = node.GetFirstChild()!;
                Node<T> lastChild = node.GetLastChild()!;
                double midpoint = 0.5 * (firstChild.PreliminaryPos + lastChild.PreliminaryPos);

                Node<T>? prevSibling = node.GetPrevSibling();
                if (prevSibling is not null)
                {
                    node.PreliminaryPos = prevSibling.PreliminaryPos + Distance;
                    node.Offset = node.PreliminaryPos - midpoint;
                }
                else
                {
                    node.PreliminaryPos = midpoint;
                }
            }
        }

        static private void ExecuteShifts<T>(Node<T> node)
        {
            double shift = 0;
            double change = 0;
            for (Node<T>? currNode = node.GetLastChild(); currNode is not null; currNode = currNode.GetPrevSibling())
            {
                currNode.PreliminaryPos += shift;
                currNode.Offset += shift;
                change += currNode.Change;
                shift += currNode.Shift + change;
            }
        }

        static private Node<T> Apportion<T>(Node<T> node, Node<T> defaultAncestor)
        {
            Node<T>? previousSibling = node.GetPrevSibling();
            if (previousSibling is not null)
            {
                Node<T>? innerRight = node;
                Node<T>? outerRight = node;
                Node<T>? innerLeft = previousSibling;
                Node<T>? outerLeft = node.GetFirstSibling()!;
                double sumInnerRight = node.Offset;
                double sumOuterRight = node.Offset;
                double sumInnerLeft = innerLeft.Offset;
                double sumOuterLeft = outerLeft.Offset;

                Node<T>? nextRight = NextRight(innerLeft);
                Node<T>? nextLeft = NextLeft(innerRight);
                while (nextRight is not null && nextLeft is not null)
                {
                    innerLeft = nextRight!;
                    innerRight = nextLeft!;
                    outerLeft = NextLeft(outerLeft)!;
                    outerRight = NextRight(outerRight)!;
                    outerRight.Ancestor = node;

                    double shift = (innerLeft.PreliminaryPos + sumInnerLeft) - (innerRight.PreliminaryPos + sumInnerRight) + Distance;
                    if (shift > 0)
                    {
                        Node<T> parent = GetAncestor(innerLeft, node, defaultAncestor);
                        MoveSubtree(parent, node, shift);
                        sumInnerRight += shift;
                        sumOuterRight += shift;
                    }

                    sumInnerLeft += innerLeft.Offset;
                    sumInnerRight += innerRight.Offset;
                    sumOuterLeft += outerLeft.Offset;
                    sumOuterRight += outerRight.Offset;

                    nextRight = NextRight(innerLeft);
                    nextLeft = NextLeft(innerRight);
                }

                if (nextRight is not null && NextRight(outerRight) is null)
                {
                    outerRight.Thread = nextRight;
                    outerRight.Offset += sumInnerLeft - sumOuterRight;
                }
                else if (nextLeft is not null && NextLeft(outerLeft) is null)
                {
                    outerLeft.Thread = nextLeft;
                    outerLeft.Offset += sumInnerRight - sumOuterLeft;
                    defaultAncestor = node;
                }
            }

            return defaultAncestor;
        }

        static private void MoveSubtree<T>(Node<T> leftParent, Node<T> rightParent, double shift)
        {
            double subtrees = rightParent.Number - leftParent.Number;
            rightParent.Change -= shift / subtrees;
            leftParent.Change += shift / subtrees;
            rightParent.Shift += shift;
            rightParent.Offset += shift;
            rightParent.PreliminaryPos += shift;
        }

        static private Node<T> GetAncestor<T>(Node<T> innerLeft, Node<T> node, Node<T> defaultAncestor)
        {
            return node.Parent!.Children!.Contains(innerLeft.Ancestor!) ? innerLeft.Ancestor! : defaultAncestor;
        }

        static private Node<T>? NextLeft<T>(Node<T> node)
        {
            return node.GetChildCount() > 0 ? node.GetFirstChild() : node.Thread;
        }

        static private Node<T>? NextRight<T>(Node<T> node)
        {
            return node.GetChildCount() > 0 ? node.GetLastChild() : node.Thread;
        }
    }
}