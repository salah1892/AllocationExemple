# SmartPlanif – Allocation Bus / Chauffeur / Receveur (C# / .NET, Clean Architecture)

> Code écrit sans compilateur disponible dans l'environnement de génération : il n'a PAS été compilé ni exécuté.
> Premier réflexe : `dotnet build && dotnet test`. Corrigez les éventuelles fautes de frappe, la logique est simple à suivre.

## Démarrage
```bash
dotnet new sln -n SmartPlanif
dotnet sln add src/*/*.csproj tests/*/*.csproj
dotnet test
dotnet run --project src/SmartPlanif.Host            # démo en mémoire (aucune base requise)
```
Pour MySQL : exécuter `sql/001_allocation_tables.sql`, renseigner `ConnectionStrings:TransportAi` et `Qualifications:Map` dans appsettings.json.

## Couches (les dépendances pointent vers l'intérieur)
| Projet | Contenu | Dépend de |
|---|---|---|
| Domain | Entités, règles dures/souples, `TripAllocator`, `ScheduleBook` (aucun package) | rien |
| Application | Cas d'usage `AllocateDayHandler`, ports (interfaces de dépôts) | Domain |
| Infrastructure | Dapper/MySQL, adaptateur en mémoire, options | Application |
| Host | Composition (DI), configuration, affichage | tout |

## Principe de l'algorithme
1. **Règles dures** (A03, A05, A08, B02, B03, B10…) : filtre. Une ressource éliminée ne revient jamais, quel que soit son score.
2. **Règles souples** (BP01…, AP01…) : chaque règle renvoie une valeur dans [0,1] ; points = poids × valeur. Les poids viennent de la configuration.
3. Voyages triés par heure de départ ; après chaque affectation, le `ScheduleBook` est mis à jour : le voyage suivant voit déjà les choix précédents.
4. Tout ou rien : un voyage sans bus, chauffeur OU receveur valide reste non affecté, avec les codes de rejet (A08: 3, B03: 2…).

## Ajouter une règle
Créer une classe `IHardRule<T>` ou `ISoftRule<T>`, l'ajouter dans `DefaultRuleSets`, écrire son test. Rien d'autre ne change.

## Limites connues / évolutions
* Heuristique gloutonne : rapide et explicable, mais pas optimale globalement. Évolution : modèle CP-SAT (Google OR-Tools) avec les mêmes règles
  comme contraintes (dures) et la somme des scores comme objectif ; le glouton sert alors de solution initiale/de secours.
* Données absentes : nombre de voyageurs prévu (B05 inactive tant que `ExpectedPassengers` est null), compatibilité catégorie/ligne (BP03 non implémentée).
* `Trip.Arrival` est dérivée de `stop_times` ; les aller/retour liés (HAVERET, TRIP_NID) sont traités comme des voyages indépendants.
* Voyages partiellement affectés (ex. bus + chauffeur mais pas de receveur) : ici ils sont réaffectés en entier ; à confirmer métier.
* Agents : `drdentr` ignorée (aucune relation avec `dragent`) ; à confirmer.
# AllocationExemple
