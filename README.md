# virtualair-api-net

This solution contains two projects:
	- VirtualAir.API: An API for accessing the data from the VirtualAir database. Such as list the observatories and doing healthchecks
	- VirtualAirDataApi: The API for accessing data from the different observatories by forwarding the request and merging the results, Observations will be aggregated  

## Dependencies
Use the project virtualair-deployment to publish to a docker container. This is done by changing the image version and merge to test/prod
	

