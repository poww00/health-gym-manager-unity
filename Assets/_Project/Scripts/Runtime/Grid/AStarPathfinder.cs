using System.Collections.Generic;
using UnityEngine;

public static class AStarPathfinder
{
    private class Node
    {
        public int x;
        public int y;
        public int gCost;
        public int hCost;
        public Node parent;
        
        public int fCost => gCost + hCost;
        
        public Node(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public static List<Vector2Int> FindPath(GridManager gridManager, Vector2Int start, Vector2Int target, bool allowTargetOccupied = false)
    {
        if (gridManager == null) return null;
        if (start == target) return new List<Vector2Int> { target };

        Node startNode = new Node(start.x, start.y);
        Node targetNode = new Node(target.x, target.y);

        List<Node> openSet = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        openSet.Add(startNode);

        int maxIterations = 2000;
        int currentIteration = 0;

        while (openSet.Count > 0)
        {
            currentIteration++;
            if (currentIteration > maxIterations)
            {
                Debug.LogWarning("[AStarPathfinder] Pathfinding exceeded max iterations. Start: " + start + ", Target: " + target);
                return null;
            }

            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(new Vector2Int(currentNode.x, currentNode.y));

            if (currentNode.x == targetNode.x && currentNode.y == targetNode.y)
            {
                return RetracePath(startNode, currentNode);
            }

            foreach (Vector2Int neighborPos in GetNeighbors(currentNode, gridManager.Width, gridManager.Height))
            {
                if (closedSet.Contains(neighborPos)) continue;

                GridCell neighborCell = gridManager.GetCell(neighborPos.x, neighborPos.y);
                if (neighborCell == null) continue;

                bool isOccupied = neighborCell.IsOccupied;
                
                bool isTargetCell = neighborPos.x == targetNode.x && neighborPos.y == targetNode.y;
                if (isOccupied && allowTargetOccupied && isTargetCell)
                {
                    isOccupied = false;
                }

                if (isOccupied) continue;

                int moveCost = GetDistance(currentNode, neighborPos);
                int newMovementCostToNeighbor = currentNode.gCost + moveCost;

                Node neighborNode = openSet.Find(n => n.x == neighborPos.x && n.y == neighborPos.y);
                
                if (neighborNode == null || newMovementCostToNeighbor < neighborNode.gCost)
                {
                    if (neighborNode == null)
                    {
                        neighborNode = new Node(neighborPos.x, neighborPos.y);
                        openSet.Add(neighborNode);
                    }
                    
                    neighborNode.gCost = newMovementCostToNeighbor;
                    neighborNode.hCost = GetDistance(neighborNode, new Vector2Int(targetNode.x, targetNode.y));
                    neighborNode.parent = currentNode;
                }
            }
        }

        return null;
    }

    private static List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(new Vector2Int(currentNode.x, currentNode.y));
            currentNode = currentNode.parent;
        }
        
        path.Reverse();
        return path;
    }

    private static int GetDistance(Node nodeA, Vector2Int posB)
    {
        int dstX = Mathf.Abs(nodeA.x - posB.x);
        int dstY = Mathf.Abs(nodeA.y - posB.y);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    private static List<Vector2Int> GetNeighbors(Node node, int width, int height)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                if (x != 0 && y != 0) continue; // Only 4-way movement to avoid cutting corners through obstacles

                int checkX = node.x + x;
                int checkY = node.y + y;

                if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                {
                    neighbors.Add(new Vector2Int(checkX, checkY));
                }
            }
        }

        return neighbors;
    }
}
