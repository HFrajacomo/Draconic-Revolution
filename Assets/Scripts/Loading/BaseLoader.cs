using System;

public abstract class BaseLoader{
	public virtual bool PreLoad(){return true;}
	public abstract bool Load();
	public virtual bool PostLoad(){return true;}
	public virtual void RunPostDeserializationRoutine(){return;}
}