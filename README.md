# Flax Outline
Basic outline post-process shader for the Flax Engine

## Installation

1. Clone this repo into `<game-project>\Plugins\FlaxPrototypeTools`

2. Add reference to the PrototypeTools project in your game by modyfying your games project file (`<game-project>.flaxproj`) as follows:


```
...
"References": [
    {
        "Name": "$(EnginePath)/Flax.flaxproj"
    },
    {
        "Name": "$(ProjectPath)/Plugins/FlaxOutline/Outline.flaxproj"
    }
]
```

3. Restart/Start Editor

4. Add the module to your `Game.Build.cs` file in the Setup function.

```
options.PrivateDependencies.Add("Outline");
```

5. Try it out! Add the OutlinePostEffect script to an actor in your scene and add the Outline shader to it.
