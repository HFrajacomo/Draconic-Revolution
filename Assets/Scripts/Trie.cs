using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trie {
    private static string initialChars = "abcdefghijklmnopqrstuvxzwy";
    private Dictionary<char, TrieNode> roots = new Dictionary<char, TrieNode>();
    private StringBuilder stringBuilder = new StringBuilder();


    public Trie(string[] items){
        this.CreateRoots();
        this.Add(items);
    }

    public void Add(string item){
        bool first = true;
        TrieNode current = new TrieNode(' ');

        foreach(char c in item.ToLower()){
            if(first){
                current = roots[c];
                first = false;
                continue;
            }

            if(!current.Contains(c))
                current.Add(new TrieNode(c));

            current = current.edges[c];
        }
    }

    public void Add(string[] items){
        foreach(string item in items){
            this.Add(item);
        }
    }

    public List<string> FindAll(string search){
        List<string> outputList = new List<string>();
        TrieNode? initialNode;

        if(search.Length <= 1){
            return outputList;
        }
        else{
            initialNode = this.FindNode(search.ToLower());
            if(initialNode == null)
                return outputList;
        }

        RecursiveSearch((TrieNode)initialNode, search.ToLower().Remove(search.Length-1), outputList);

        return outputList;
    }

    private void RecursiveSearch(TrieNode node, string output, List<string> outputList){
        foreach(TrieNode next in node.edges.Values){
           RecursiveSearch(next, output + node.val, outputList);
        }

        if(node.edges.Count == 0)
            outputList.Add(output + node.val);
    }

    /*
    Finds the exact node that corresponds to a search query
    returns null if node doesn't exist
    */
    public TrieNode? FindNode(string search){
        bool first = true;
        TrieNode current = new TrieNode(' ');

        foreach(char c in search){
            if(first){
                first = false;
                if(roots.ContainsKey(c))
                    current = roots[c];
                else
                    return null;
                continue;
            }

            if(current.Contains(c))
                current = current.edges[c];
            else
                return null;
        }

        return current;
    }


    private void CreateRoots(){
        foreach(char c in Trie.initialChars){
            roots.Add(c, new TrieNode(c));
        }
    }
}




public struct TrieNode {
    public char val;
    public Dictionary<char, TrieNode> edges;

    public TrieNode(char val){
        this.val = val;
        this.edges = new Dictionary<char, TrieNode>();
    }

    public void Add(TrieNode node){
        this.edges.Add(node.val, node);
    }

    public bool Contains(char c){
        return this.edges.ContainsKey(c);
    }

    public override int GetHashCode(){
        return (int)this.val;
    }

    public override string ToString(){
        return this.val.ToString();
    }
}
