using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * BiMap or Bi-directional Map
 * */

public class BiMap<T, U>
{
    private Dictionary<T, U> tuMap;
    private Dictionary<U, T> utMap;

    public BiMap(){
        this.tuMap = new Dictionary<T, U>();
        this.utMap = new Dictionary<U, T>();
    }

    public U Get(T element){return this.tuMap[element];}
    public T Get(U element){return this.utMap[element];}

    public bool Contains(T element){return this.tuMap.ContainsKey(element);}
    public bool Contains(U element){return this.utMap.ContainsKey(element);}

    public bool IsEmpty(){return this.tuMap.Count == 0;}

    public void Add(T t, U u){
        this.utMap.Add(u, t);    
        this.tuMap.Add(t, u);
    }

    public void Add(U u, T t){
        this.utMap.Add(u, t);    
        this.tuMap.Add(t, u);    
    }

    public void Remove(T t){
        if(this.Contains(t)){
            this.tuMap.Remove(t);
        }
    }

    public void Remove(U u){
        if(this.Contains(u)){
            this.utMap.Remove(u);
        }
    }

    public void Clear(){
        this.tuMap = new Dictionary<T, U>();
        this.utMap = new Dictionary<U, T>();
    }
}
