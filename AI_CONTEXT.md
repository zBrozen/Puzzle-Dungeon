# Contexte du Projet : Puzzle-Dungeon

Ce document sert de référence pour les intelligences artificielles (IA) afin de comprendre la vision, les mécaniques et les objectifs du projet "Puzzle-Dungeon".

## 🎮 Concept Principal
Jeu d'énigmes dans un environnement 3D fortement inspiré du **Donjon de la Tour des Cieux (Sky Keep)** de *The Legend of Zelda: Skyward Sword*. 
Le gameplay repose sur la résolution de puzzles spatiaux et logiques, avec ou sans l'aide d'objets, pour progresser dans le donjon.

## 🗺️ Structure et Progression (Level Design)
- **Objectif de Scope :** Environ 2 énigmes par salle (pour garder un scope de développement réaliste et un bon rythme).
- **Architecture :** Le donjon est composé de 3 grandes salles principales, ainsi que de plus petites salles "énigmes" permettant d'obtenir des accès ou des récompenses.
- **Inventaire et Progression :**
  - **Objets classiques :** Des clés (pour ouvrir les portes verrouillées). Le **Scarabée** (pour activer des mécanismes à distance) s'obtient plutôt vers le milieu du jeu.
  - **Pièces de Taquin :** Il y a 6 pièces au total pour manipuler l'agencement des 3 salles. Chaque salle inclut 1 pièce de base, il y a donc 3 pièces supplémentaires à trouver en résolvant des énigmes. Ces pièces ou l'accès à la stèle s'obtiennent également plus tard (milieu de partie), et non dès le début.
- **Séquence d'Introduction (Tutoriel) :**
  - **Parcours d'approche :** Un chemin simple où le joueur peut tomber, introduisant le déplacement.
  - **Tuto Bloc :** Le joueur apprend à pousser un bloc pour s'en servir de plateforme/pont afin d'atteindre la première grande salle.
  - **Première Grande Salle :** Le joueur ramène ce bloc dans la salle pour le placer sur un bouton pression et ouvrir une porte (le poids du joueur n'étant pas idéal pour traverser la salle pendant que la porte est ouverte).

## 🧩 Mécaniques et Énigmes Actuelles
1. **Puzzle Principal (Manipulation du donjon) :** 
   - Le donjon possède une architecture verticale composée de seulement 3 salles.
   - Le joueur interagit avec une stèle (façon taquin) pour déplacer les salles. La grille possède un emplacement vide permettant ce déplacement afin de créer de nouveaux passages.
2. **Boutons Pression :** 
   - Interrupteurs au sol qui activent un mécanisme (ex: ouverture de porte) de manière **temporaire** tant que le joueur (ou un objet) se trouve dessus.
3. **Blocs à Pousser :** 
   - Blocs interactifs pouvant servir :
     - De **plateforme** pour atteindre des zones en hauteur.
     - De **poids** en les poussant sur un bouton pression pour maintenir un mécanisme ouvert de manière permanente.
4. **Objet : Le Scarabée :** 
   - Un objet (projectile) que le joueur peut lancer et contrôler à distance.
   - Dédié à des zones de parcours spécifiques (obstacles, passages étroits) pour aller frapper une cible inaccessible et valider une énigme.

## ⚙️ Objectif Technique Majeur : Variabilité des Runs (JSON)
- **Génération Data-Driven :** Le jeu doit permettre à des fichiers JSON de configurer l'accessibilité des énigmes lors d'une session (run). 
- **Objectif :** Éviter que les joueurs aient exactement le même parcours. 
- **Application :** Une grande salle sera construite avec une abondance d'énigmes différentes permettant toutes d'accomplir la même tâche. Le JSON se chargera de n'en rendre qu'une seule applicable/accessible par le joueur.
- *Priorité actuelle :* La mise en place de ce système est secondaire ; la priorité absolue actuelle est la **création et validation des mécaniques de base**.

## 🤖 Mode d'Interaction et Personas
L'utilisateur peut demander à l'IA d'adopter des rôles spécifiques selon les besoins du moment. Lors de vos réponses, adaptez votre expertise au rôle demandé :

* **Persona `[DEV]` (Développeur) :** 
  - Focus sur l'architecture du code, les scripts, l'intégration dans le moteur de jeu.
  - Aide sur l'implémentation des mécaniques (contrôleur 3D, physique des blocs, vol du scarabée, lecture des JSON).
* **Persona `[LEVEL DESIGN]` (Level Designer) :** 
  - Focus sur la conception spatiale, l'inspiration et la création d'énigmes.
  - Aide pour agencer les salles, utiliser les mécaniques de façon intelligente, et créer un "flow" intéressant pour le joueur (l'utilisateur étant moins expérimenté dans ce domaine).
* **Persona `[GAME DESIGN]` (Game Designer) :** 
  - Focus sur l'équilibre, la cohérence des mécaniques entre elles et l'expérience utilisateur globale.

---
**Pour l'utilisateur :** 
Lors d'une nouvelle conversation, vous pouvez fournir ce fichier à l'IA et commencer par :
> *"Prends connaissance de ce fichier de contexte. Pour cette session, j'ai besoin que tu agisses en tant que **[DEV / LEVEL DESIGN]**. Voici mon problème : ..."*
