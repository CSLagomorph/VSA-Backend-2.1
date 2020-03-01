#  Course Network API
This is the README for the course network API. This page describes the inputs and outputs
for the API. This page also describes the caching mechanism used to store the course network 
on the locally on the server side machine.

## Methods
The only method for this API is the GET method with one parameter. An example of a call to this 
endpoint are as follow:

* The curl command: (Make sure to change the localhost name to the actual DNS when deployed.)

```
curl -X GET "https://localhost:44390/v1/CourseNetwork?course={CourseID}" -H "accept: text/plain"
```

* The HTTP URL:

```
https://localhost:44390/v1/CourseNetwork?course={CourseID}
```

The `{CourseID}` in both lines are the user defined parameter, where the course ID is a valid ID stored in the VSADev database. (Which at the time is the database that this API pulls its resources from.)

### Return Value

The return value of the GET Method is a JSON object that outlines an adjacency list of all the prerequisites for the given course ID.

To be able to use the JSON object returned from the API the receiving code must have access to CourseNode.cs. This is vital since the way you unpackage the data is by:



`courseNetwork = JsonConvert.DeserializeObject<List<CourseNode>>(allCourses);`



where `allcourses ` is the value returned from the API.



The design of this object is heavily influenced by the object structure of the CourseNode.cs in the Scheduler CS project and the Course Network CS project. 

_If you want to understand more of the underlying structure of the return value please refer to CourseNode.cs here [VSA-Backend](https://github.com/VSAResearchGroup/VSA-Backend-2.1/blob/master/Algorithm-2.0-master/CourseNode.cs)

### Caching 

The Course Network Object is cached on the server for a sliding lifespan of 5 seconds. If the API request results in a hit then the API must be create, index, and build a course network from scratch. So keeping the API alive can result in a large speedup of request resolution.



### Database

The course network accesses the Microsoft SQL database through the SQL Connection in the System packages offered by the .net framework. The Connection is established in the Course Network file. The connection string is accessed via a file called DBConnection.json. This file is in the .gitignore since it contains the connection string. 



If you choose to test/deploy this API please create a file and fill it with the following contents:

```json
{
    "DBConnectionString":"<Your DB Connection String>"
}
```



