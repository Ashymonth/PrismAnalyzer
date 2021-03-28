# PrismAnalyzer
Allows you to automatically wrap entity class in binding models

## Convention
For the analyzer to work, you must have the following code structure.

You should have base class for all entities. The class must contain `entity` in the name.

You should have base class for all models. The class must contain `model` in the name.

### Example structure

```
public abstract class EntityBase
{
    public int Id { get; set; }
}

public abstract class ModelBase<TEntity> : BindableBase where TEntity : EntityBase
{
    protected ModelBase(TEntity entity)
    {
        Entity = entity;
    }
        
    public TEntity Entity { get; }
}
```

![Analyzer Demo](readme_git.gif)
