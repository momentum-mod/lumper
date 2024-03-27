![Momentum Mod](https://momentum-mod.org/assets/images/logo.svg)

> *Momentum Mod is a standalone game built on the Source Engine, aiming to centralize movement gametypes found in CS:S, CS:GO, and TF2.*

Lumper is a .NET based tool developed by the Momentum Mod Team for inspecting and manipulating various "lumps" of BSP
files, the Source engine's compiled map format.

It can read and write the following
- **Entity lump**, including entity IO
- **Pakfile lump**: the packed assets and other files. Supports viewing VTF images

## Tasks
TODO

### Momentum
On Momentum frequently need to adjust BSPs slightly when porting. Out of respect for mappers we avoid modifying the original BSP
as much as we possibly can. Manipulating existing BSPs is much less intrusive than decompiling, adjusting, and
recompiling. Lumper lets us:
- scan pakfile for unwanted assets
- tweaking entities 

Also, we use Lumper as a fast way for moderators to examine new BSPs during map submission.

### Contributing
We are happy to accept contributions. Whilst we prioritize Momentum-related use-cases ourselves, we hope this tool is
helpful to anyone using Source 1, and we're happy to accept contributions and bug reports from people not involved with
Momentum.

Besides this repo, the best way to contact us is on the [Momentum Mod Discord server](https://discord.gg/momentummod), 
ideally in the `#tooling` channel.
