using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Threading;
using System.IO;

namespace FacesApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FacesController : ControllerBase
    {
        const string SUBSCRIPTION_KEY = "YOUR SUSCRIPTION KEY";
        const string ENDPOINT = "YOUR ENDPOINT";


        IFaceClient client = new FaceClient(new ApiKeyServiceClientCredentials(SUBSCRIPTION_KEY)) { Endpoint = ENDPOINT };
        


        public FacesController(){

         }


        [HttpDelete("deletePersonGroup")]
        public async Task deletePersonGroup([FromForm] string personGroupId){

               client.PersonGroup.DeleteAsync(personGroupId);
            
        }
        [HttpDelete("deletePerson")]
        public async Task deletePerson([FromForm] string personGroupId, [FromForm] string personId)
        {
            client.PersonGroupPerson.DeleteAsync(personGroupId, new Guid(personId));

        }




        [HttpPost("identifyPerson")]
        public async Task<Object> identifyPerson([FromForm] string groupId, [FromForm] string imageUrl)
        {


          IList<DetectedFace> faces =
                await client.Face.DetectWithUrlAsync(url: imageUrl, returnFaceId: true, detectionModel: DetectionModel.Detection02);


            IList<Guid> faceIds = new List<Guid>();
             foreach (var face in faces)
              {
                  faceIds.Add((Guid)face.FaceId);
              }

          var response =  await client.Face.IdentifyAsync(faceIds, personGroupId: groupId);

            List<Person> persons = new List<Person>();
            foreach (var candidates in response)
            {
                foreach (var item in candidates.Candidates)
                {
                    persons.Add(await client.PersonGroupPerson.GetAsync(groupId,item.PersonId));
                }
            }
            return persons;
        }

        [HttpPost("addPersonToGroup")]
        public async Task<Person> addPersonToGroup([FromForm] string groupId, [FromForm] string personName)
        {
            Person response = await client.PersonGroupPerson.CreateAsync(groupId, name: personName, userData: personName);
            return response;
        }


        [HttpPost("addPersonFace")]
        public async Task<PersistedFace> addPersonFace([FromForm] string groupId, [FromForm] string personId, [FromForm] string urlFace)
        {
            Guid guid = new Guid(personId);
            var response = await client.PersonGroupPerson.AddFaceFromUrlAsync(personGroupId:groupId,personId:guid,url:urlFace);
            await client.PersonGroup.TrainAsync(groupId);

            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await client.PersonGroup.GetTrainingStatusAsync(groupId);
                Console.WriteLine($"Training status: {trainingStatus.Status}.");
                if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
            }

            return response;
        }

        [HttpPost("getFace")]
        public async Task<PersistedFace> getFace([FromForm] string groupId, [FromForm] string personId, [FromForm] string idFace)
        {
            Guid personGuid = new Guid(personId);
            Guid faceGuid = new Guid(idFace);
            var response = await client.PersonGroupPerson.GetFaceAsync(groupId,personGuid,faceGuid);
            return response;
        }

        [HttpPost("getGroups")]
        public async Task<IList<PersonGroup>> getPersonsGroup()
        {
            var response = await client.PersonGroup.ListAsync();
            return response;
        }

        [HttpPost("getPersonsOfGroup")]
        public async Task<IList<Person>> getPersonsOfGroup([FromForm] string groupId)
        {
            var response = await client.PersonGroupPerson.ListAsync(groupId);
            return response;
        }


        [HttpPost("makePresonGroup")]
        public async Task makePresonGroupGetAsync([FromForm] string groupName, [FromForm] string userData)
        {
            string personGroupId = Guid.NewGuid().ToString();
            await client.PersonGroup.CreateAsync(personGroupId, groupName,userData);
        }

    }
}
