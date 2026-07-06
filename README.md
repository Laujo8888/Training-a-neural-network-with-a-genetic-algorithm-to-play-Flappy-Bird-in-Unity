# Training a neural network with a genetic algorithm to play Flappy Bird in Unity



















https://github.com/user-attachments/assets/8c35d468-9ebe-4150-bcfe-27641d7b1143






## how to use it
First of all Unity and the tools in accordance need to be installed (This project was created in Unity 6000.0.35f1, so there might be the need to download another version).

Move where you want to copy the repo to with ```cd YOURPATH``` 

Then simply run ```git clone https://github.com/Laujo8888/Training-a-neural-network-with-a-genetic-algorithm-to-play-Flappy-Bird-in-Unity ```

Afterwards open the Unity Hub and under "Add", "Add project from disk" and select the freshly copied repo and open it in the Hub. 

*Note that cranking up the speed will cause rapid flickering and flashing!*


## general
As shown in the picture, the blue dots are the birds. Each of them has a neural network as a brain, the spikes function as the obstacles and are randomly generated each time, birds die when touching either the ceiling, the floor or the spikes.
<img width="3832" height="1766" alt="Screenshot 2026-07-06 193826" src="https://github.com/user-attachments/assets/97131c21-e6ef-446e-a8a4-271a363659e6" />
At the top one can see the save and load buttons, when the save button is pressed, after the current generation dies out, the "brain" of the best scoring Bird will be saved as a .json to Application.persistentDataPath, which is shown underneath the save and load buttons. When the load button is pressed, the current next generation will be loaded with the saved brain, though mutation will still be applied.

The slider next to the load button is the speed slider, using which the speed can be changed from 0x - essentially freezing the game - all the way up to 12x times speed.

The menu button takes you to the menu, note that the progress won't be saved, using which you can restart the training.

The slider underneath the Menu button lets you zoom in and out.

As soon as the first two generations are over, 4 overlaying graphs will appear, they represent the numbers of the same colour that are represented in the bottom right corner, which show data based on the scores of the previous generations.

By pressing the 1-4 keys each graph can be hidden individually, by pressing the 0 key all UI except for the graphs will be hidden.



## neural network
The Network consists of
* 4 input nodes
* 8 hidden nodes
* 1 output node
<img width="707" height="500" alt="neuralNetwork1" src="https://github.com/user-attachments/assets/2ad8124d-e9f2-444e-af9d-f4d11a6490c9" />


The first input node gets the Y position of the bird, the second one the bird's vertical velocity, the third one the x distance to the next pipe and the fourth one the y distance to the gap between the spikes.

If the value of the output is greater than 0.5 the bird jumps.

The sigmoid function was used as the activation function.
## genetic algorithm
In this case all weights and biases are the genome, every generation starts with 100 birds.

The fitness function is $Fitness(bird)=pipesSurvived^2\times 10+timeSurvived \times 0.1 +timeAtGapHeight\times 0.5$ as this worked the best overall, there is, though, still a lot of fitness related code left, that is now unused eg. novelty search.

The probability to pass on the genome of a bird is based on the ratio of the birds fitness to the total fitness, so if the total fitness is 500 and a bird got a fitness score of 100 they have a 20% chance of passing on their genome, additionally the genomes of the best few birds of the last generation are guaranteed to be passed on.

To help the birds get out of local maxima or if they are stuck in general, if the maximum average fitness did not increase in the last 30 generations, 50 of the 100 birds will get "reshuffled", which means they get random weights and biases instead of having a genome of the last generation passed on to them, also the mutation rate shrinks in accordance to the generation count, as to promote more exploration in the beginning and optimization in the end.
