using InnovateFPGA2021_WebApp.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InnovateFPGA2021_WebApp.Helper
{
	public interface IIoTHubDps
	{
		string IoTHubHubNameGet(string deviceConnectionString);
		Task<IoTHubDeviceListViewModel> IoTHubDeviceListGet();
		Task<IoTHubModuleListViewModel> IoTHubModuleListGet(string deviceId);
		Task<Device> IoTHubDeviceGet(string deviceId);
		Task<Twin> IoTHubDeviceTwinGet(string deviceId);
		Task<bool> IoTHubDeviceDelete(string deviceId);
		Task<bool> IoTHubDeviceCreate(string deviceId, bool isEdge);
		Task<bool> IoTHubDeviceCheck(string deviceId);
		Task<DpsEnrollmentListViewModel> DpsEnrollmentListGet();
		Task<DPS_ENROLLMENT_DATA> DpsEnrollmentGet(string enrollmentId, bool isGroup);
		Task<AttestationMechanism> DpsAttestationMethodGet(string registrationId, bool isGroup);
		Task<bool> DpsEnrollmentCreate(string registrationId, bool isGroup, bool isEdge);
		Task<bool> DpsEnrollmentDelete(string registrationId, bool isGroup);
	}

	public class IoTHubDps : IIoTHubDps
	{
		private readonly ILogger<IoTHubDps> _logger;
		private readonly AppSettings _appSettings;
		private readonly RegistryManager _registryManager;
		private readonly ProvisioningServiceClient _provisioningServiceClient;

		public IoTHubDps(IOptions<AppSettings> config, ILogger<IoTHubDps> logger)
		{
			_logger = logger;
			_appSettings = config.Value;
			_registryManager = RegistryManager.CreateFromConnectionString(_appSettings.IoTHub.ConnectionString);
			_provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(_appSettings.Dps.ConnectionString);
		}

		/**********************************************************************************
		* Get a name of IoT Hub from Connection String
		*********************************************************************************/
		public string IoTHubHubNameGet(string deviceConnectionString)
		{
			return deviceConnectionString.Split(';')[0].Split('=')[1];
		}

		#region IOTHUB
		/**********************************************************************************
         * Get list of devices from IoT Hub
         *********************************************************************************/
		public async Task<IoTHubDeviceListViewModel> IoTHubDeviceListGet()
		{
			var iothubDeviceListViewModel = new IoTHubDeviceListViewModel();

			try
			{
				IQuery query = _registryManager.CreateQuery("select * from devices");

				while (query.HasMoreResults)
				{
					var twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);
					foreach (var twin in twins)
					{
						_logger.LogInformation($"Found a device : {twin.DeviceId}");
						var modelId = string.IsNullOrEmpty(twin.ModelId) ? "No Model ID" : twin.ModelId;
						iothubDeviceListViewModel.Devices.Add(new IoTHubDeviceViewModel
						{
							DeviceId = twin.DeviceId,
							AuthenticationType = twin.AuthenticationType.ToString(),
							ModelId = modelId,
							Status = twin.Status.ToString(),
							IsEdge = twin.Capabilities.IotEdge
						});
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in IoTHubGetDeviceList() : {e.Message}");
			}

			return iothubDeviceListViewModel;
		}

		/**********************************************************************************
		 * Retrieves the specified Device object.
		 *********************************************************************************/
		public async Task<Device> IoTHubDeviceGet(string deviceId)
		{
			Device device = null;
			try
			{
				device = await _registryManager.GetDeviceAsync(deviceId);
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in IoTHubDeviceGet() : {e.Message}");
			}
			return device;
		}

		/**********************************************************************************
		 * Gets IoT Edge Modules
		 *********************************************************************************/
		public async Task<IoTHubModuleListViewModel> IoTHubModuleListGet(string deviceId)
		{
			var iothubModuleListViewModel = new IoTHubModuleListViewModel();

			try
			{
				IEnumerable<Module> modules;
				_logger.LogDebug($"Retrieving Modules for {deviceId}");
				modules = await _registryManager.GetModulesOnDeviceAsync(deviceId);

				foreach (var module in modules)
				{
					_logger.LogInformation($"Found a module : {module.Id}");

					var moduleTwin = await IoTHubModuleTwinGet(deviceId, module.Id).ConfigureAwait(false);

					var modelId = string.IsNullOrEmpty(moduleTwin.ModelId) ? "No Model ID" : moduleTwin.ModelId;

					iothubModuleListViewModel.Modules.Add(new IoTHubModuleViewModel
					{
						ModuleId = module.Id,
						ModelId = modelId,
						Status = module.ConnectionState.ToString(),
					});
				}
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in GetModulesOnDeviceAsync() : {e.Message}");
				throw e;
			}
			return iothubModuleListViewModel;
		}

		/**********************************************************************************
		 * Gets Twin from IoT Hub
		 *********************************************************************************/
		public async Task<Twin> IoTHubDeviceTwinGet(string deviceId)
		{
			Twin twin = null;

			try
			{
				_logger.LogDebug($"Retrieving Twin for {deviceId}");
				twin = await _registryManager.GetTwinAsync(deviceId);
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in IoTHubDeviceTwinGet() : {e.Message}");
				throw e;
			}
			return twin;
		}

		/**********************************************************************************
		 * Gets Twin from IoT Hub
		 *********************************************************************************/
		private async Task<Twin> IoTHubModuleTwinGet(string deviceId, string moduleId)
		{
			Twin twin = null;

			try
			{
				_logger.LogDebug($"Retrieving Twin for {moduleId}");
				twin = await _registryManager.GetTwinAsync(deviceId, moduleId);
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in IoTHubDeviceTwinGet() : {e.Message}");
				throw e;
			}
			return twin;
		}

		public async Task<bool> IoTHubDeviceCheck(string deviceId)
		{
			IQuery query = _registryManager.CreateQuery($"select * from devices");

            try
            {
				while (query.HasMoreResults)
				{
					var twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);
					foreach (var twin in twins)
					{
						if (twin.DeviceId.Equals(deviceId))
                        {
							_logger.LogInformation($"Found a device : {twin.DeviceId}");
							return true;
						}
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in IoTHubDeviceTwinGet() : {e.Message}");
			}

			return false;
		}

		/**********************************************************************************
         * Deletes a previously registered device from IoT Hub
         *********************************************************************************/
		public async Task<bool> IoTHubDeviceDelete(string deviceId)
		{
			try
			{
				_logger.LogDebug($"Removing {deviceId}");
				await _registryManager.RemoveDeviceAsync(deviceId.ToString()).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in IoTHubDeviceDelete() : {e.Message}");
				return false;
			}
			return true;
		}

		/**********************************************************************************
         * Register a new device with IoT Hub
         *********************************************************************************/
		public async Task<bool> IoTHubDeviceCreate(string deviceId, bool isEdge)
		{
			Device device = null;

			device = await _registryManager.GetDeviceAsync(deviceId.ToString());

			if (device == null)
			{
				_logger.LogDebug($"Creating a new device : '{deviceId}'");

				var deviceTemplate = new Device(deviceId.ToString());
				deviceTemplate.Capabilities = new DeviceCapabilities { IotEdge = isEdge };

				device = await _registryManager.AddDeviceAsync(deviceTemplate);

				if (device != null)
				{
					// make sure GetIoTHubDevices() returns the new device
					bool bFound = false;

					while (!bFound)
					{
						bFound = await IoTHubDeviceCheck(deviceId);


						if (bFound != true)
						{
							Thread.Sleep(100);
						}
					}
				}
				else
				{
					throw new IotHubException($"Failed to create {deviceId} in IoT Hub");
				}
			}
			else
			{
				_logger.LogWarning($"Device already exist : '{deviceId}'");
				throw new DeviceAlreadyExistsException(deviceId);
			}

			return device != null;
		}

		#endregion

		#region DPS

		/**********************************************************************************
         * Get list of DPS Enrollments
         *********************************************************************************/
		public async Task<DpsEnrollmentListViewModel> DpsEnrollmentListGet()
		{
			var enrollmentList = new DpsEnrollmentListViewModel();
			QuerySpecification querySpecification;

			try
			{
				querySpecification = new QuerySpecification("SELECT * FROM enrollments");
				using (Query query = _provisioningServiceClient.CreateEnrollmentGroupQuery(querySpecification))
				{
					while (query.HasNext())
					{
						QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
						foreach (EnrollmentGroup enrollment in queryResult.Items)
						{
							// we only support symmetric key for now
							if (enrollment.Attestation.GetType().Name.Equals("SymmetricKeyAttestation"))
							{
								enrollmentList.Enrollments.Add(new EnrollmentViewModel { RegistrationId = enrollment.EnrollmentGroupId, isGroup = true });
							}
						}
					}
				}

				querySpecification = new QuerySpecification("SELECT * FROM enrollments");
				using (Query query = _provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification))
				{
					while (query.HasNext())
					{
						QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
						foreach (IndividualEnrollment enrollment in queryResult.Items)
						{
							// we only support symmetric key for now
							if (enrollment.Attestation.GetType().Name.Equals("SymmetricKeyAttestation"))
							{
								enrollmentList.Enrollments.Add(new EnrollmentViewModel { RegistrationId = enrollment.RegistrationId, isGroup = false });
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in GetDpsEnrollments() : {e.Message}");
				throw new ProvisioningServiceClientHttpException($"Failed to retrieve enrollments", e);
			}

			enrollmentList.Enrollments = enrollmentList.Enrollments.OrderBy(s => s.RegistrationId).ToList();

			return enrollmentList;
		}

		private async Task<EnrollmentGroup> GetDpsEnrollmentGroup(string enrollmentId)
        {
			EnrollmentGroup enrollment = null;

			try
			{
				QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");
				using (Query query = _provisioningServiceClient.CreateEnrollmentGroupQuery(querySpecification))
				{
					while (query.HasNext() && enrollment == null)
					{
						QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);

						foreach (EnrollmentGroup item in queryResult.Items)
						{
							if (item.EnrollmentGroupId.Equals(enrollmentId))
							{
								enrollment = item;
								break;
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in GetDpsGroupEnrollment() : {e.Message}");
				throw new ProvisioningServiceClientHttpException($"Failed to retrieve group enrollments", e);
			}

			return enrollment;
		}

		private async Task<IndividualEnrollment> GetDpsEnrollmentIndividual(string enrollmentId)
		{
			IndividualEnrollment enrollment = null;

			try
			{
				QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");

				using (Query query = _provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification))
				{
					while (query.HasNext() && enrollment == null)
					{
						QueryResult queryResults = await query.NextAsync().ConfigureAwait(false);

						foreach (IndividualEnrollment item in queryResults.Items)
						{
							_logger.LogInformation($"GetDpsIndividualEnrollment found enrollment : {item}");

							if (item.RegistrationId.Equals(enrollmentId))
							{
								enrollment = item;
								break;
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in GetDpsIndividualEnrollment() : {e.Message}");
				throw new ProvisioningServiceClientHttpException($"Failed to retrieve individual enrollment {enrollmentId}", e);
			}

			return enrollment;
		}

		/**********************************************************************************
		 * Retrieve attestation from DPS
		 *********************************************************************************/
		public async Task<AttestationMechanism> DpsAttestationMethodGet(string registrationId, bool isGroup)
		{
			AttestationMechanism attestation = null;

			try
			{
				if (isGroup)
				{
					attestation = await _provisioningServiceClient.GetEnrollmentGroupAttestationAsync(registrationId).ConfigureAwait(false);
				}
				else
				{
					attestation = await _provisioningServiceClient.GetIndividualEnrollmentAttestationAsync(registrationId).ConfigureAwait(false);
				}
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in GetDpsAttestationMechanism() : {e.Message}");
				throw new ProvisioningServiceClientHttpException($"Failed to retrieve attestation mechanism for {registrationId}", e);
			}

			return attestation;
		}

		public async Task<DPS_ENROLLMENT_DATA> DpsEnrollmentGet(string enrollmentId, bool isGroup)
		{
			DPS_ENROLLMENT_DATA enrollmentData = null;
			EnrollmentGroup enrollmentGroup = null;
			IndividualEnrollment enrollmentIndividual = null;

			if (isGroup)
			{
				try
				{
					enrollmentGroup = await GetDpsEnrollmentGroup(enrollmentId);

					if (enrollmentGroup != null)
                    {
						enrollmentData = new DPS_ENROLLMENT_DATA();

						enrollmentData.registrationId = enrollmentGroup.EnrollmentGroupId;
						enrollmentData.status = enrollmentGroup.ProvisioningStatus.ToString();
						enrollmentData.isGroup = true;
						if (enrollmentGroup.Capabilities != null)
						{
							enrollmentData.isEdge = enrollmentGroup.Capabilities.IotEdge;
						}
						else
						{
							enrollmentData.isEdge = false;
						}
					}
				}
				catch (Exception e)
				{
					_logger.LogError($"Exception in GetDpsEnrollmentGroup() : {e.Message}");
					throw new ProvisioningServiceClientHttpException($"Failed to retrieve group enrollments", e);
				}
			}
			else
			{
				try
				{
					enrollmentIndividual = await GetDpsEnrollmentIndividual(enrollmentId);
					if (enrollmentIndividual != null)
					{
						enrollmentData = new DPS_ENROLLMENT_DATA();

						enrollmentData.registrationId = enrollmentIndividual.RegistrationId;
						enrollmentData.status = enrollmentIndividual.ProvisioningStatus.ToString();
						enrollmentData.isGroup = false;
						if (enrollmentIndividual.Capabilities != null)
                        {
							enrollmentData.isEdge = enrollmentIndividual.Capabilities.IotEdge;
						}
                        else
                        {
							enrollmentData.isEdge = false;
						}
					}
				}
				catch (Exception e)
				{
					_logger.LogError($"Exception in GetDpsEnrollmentIndividual() : {e.Message}");
					throw new ProvisioningServiceClientHttpException($"Failed to retrieve individual enrollments", e);
				}
			}

			return enrollmentData;
		}

		/**********************************************************************************
		 * Create a new DPS Enrollment
		 *********************************************************************************/
		public async Task<bool> DpsEnrollmentCreate(string registrationId, bool isGroup, bool isEdge)
		{
			string symmetricKey = "";
			bool bCreated = false;

			try
			{
				Attestation attestation = new SymmetricKeyAttestation(symmetricKey, symmetricKey);

				if (isGroup)
				{
					EnrollmentGroup groupEnrollment = new EnrollmentGroup(registrationId, attestation);

					if (isEdge)
                    {
						groupEnrollment.Capabilities = new DeviceCapabilities { IotEdge = isEdge };
					}

					var newEnrollmentGroup = await _provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(groupEnrollment).ConfigureAwait(false);

					if (newEnrollmentGroup != null)
					{
						bCreated = true;
					}
				}
				else
				{
					IndividualEnrollment individualEnrollment = new IndividualEnrollment(registrationId, attestation);
					individualEnrollment.DeviceId = registrationId;

					if (isEdge)
					{
						individualEnrollment.Capabilities = new DeviceCapabilities { IotEdge = isEdge };
					}

					var newEnrollmentIndividual = await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);

					if (newEnrollmentIndividual != null)
					{
						bCreated = true;
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in DpsEnrollmentCreate() : {e.Message}");
				throw new ProvisioningServiceClientHttpException($"Failed to create enrollment {registrationId}", e);
			}
			return bCreated;
		}

		/**********************************************************************************
		 * Delete DPS Enrollment
		 *********************************************************************************/
		public async Task<bool> DpsEnrollmentDelete(string enrollmentName, bool isGroup)
		{
			bool bDeleted = false;
			try
			{
				_logger.LogDebug($"Deleting enrollment {enrollmentName}");

				if (isGroup)
				{
					await _provisioningServiceClient.DeleteEnrollmentGroupAsync(enrollmentName).ConfigureAwait(false);
				}
				else
				{
					await _provisioningServiceClient.DeleteIndividualEnrollmentAsync(enrollmentName).ConfigureAwait(false);
				}
				bDeleted = true;
			}
			catch (Exception e)
			{
				_logger.LogError($"Exception in DpsEnrollmentDelete() : {e.Message}");
				throw new ProvisioningServiceClientHttpException($"Failed to delete enrollment {enrollmentName}", e);
			}
			return bDeleted;
		}
		#endregion
	}
}
