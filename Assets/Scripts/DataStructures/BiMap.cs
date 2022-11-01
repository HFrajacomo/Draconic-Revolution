using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * BiMap or Bi-directional Map
 * */

public class BiMap<T, U>
{
    private Dictionary<T, List<U>> tuMap;
    private Dictionary<U, List<T>> utMap;

    public BiMap(){
        this.tuMap = new Dictionary<T, List<U>>();
        this.utMap = new Dictionary<U, List<T>>();
    }

    public bool Contains(T element){return this.tuMap.ContainsKey(element);}
    public bool Contains(U element){return this.utMap.ContainsKey(element);}

    public bool IsEmpty(T t){return this.tuMap[t].Count > 0;}
    public bool IsEmpty(U u){return this.utMap[u].Count > 0;}

    public void Add(T t, U u){
        if(this.Contains(t)){
            this.tuMap[t].Add(u);
            this.utMap[u].Add(t);
        }
        else{
            this.tuMap.Add(t, new List<U>(){u});
            this.utMap.Add(u, new List<T>(){t});
        }
    }

    public void Add(U u, T t){
        if(this.Contains(u)){
            this.utMap[u].Add(t);
            this.tuMap[t].Add(u);
        }
        else{
            this.utMap.Add(u, new List<T>(){t});    
            this.tuMap.Add(t, new List<U>(){u});    
        }
    }

    public void Remove(T t){
        if(this.Contains(t)){
            this.tuMap.Remove(t);

            foreach(List<T> tList in utMap.Values){
                tList.Remove(t);
            }
        }
    }

    public void Remove(U u){
        if(this.Contains(u)){
            this.utMap.Remove(u);

            foreach(List<U> uList in tuMap.Values){
                uList.Remove(u);
            }
        }
    }

    public void Clear(){
        this.tuMap = new Dictionary<T, List<U>>();
        this.utMap = new Dictionary<U, List<T>>();
    }
}
