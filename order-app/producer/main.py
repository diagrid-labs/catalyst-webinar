from dapr.clients import DaprClient
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from cloudevents.sdk.event import v1
import logging
import grpc
import requests
import os

app = FastAPI()

logging.basicConfig(level=logging.INFO)


class Order(BaseModel):
    orderId: int


class CloudEvent(BaseModel):
    datacontenttype: str
    source: str
    topic: str
    pubsubname: str
    data: dict
    id: str
    specversion: str
    tracestate: str
    type: str
    traceid: str


# Set up required inputs for http client to perform service invocation
base_url = os.getenv('DAPR_HTTP_ENDPOINT', 'http://localhost')
dapr_api_token = os.getenv('DAPR_API_TOKEN', '')
pubsub_name = os.getenv('PUBSUB_NAME', 'pubsub')
pubsub_topic = os.getenv('PUBSUB_TOPIC', 'orders')

@app.post('/orders')
async def publish_orders(order: Order):
    with DaprClient() as d:
        try:
            result = d.publish_event(
                pubsub_name=pubsub_name,
                topic_name=pubsub_topic,
                data=order.model_dump_json(),
                data_content_type='application/json',
            )
            logging.info('Publish Successful. Order published: %s' %
                         order.orderId)
            return {'success': True}
        except grpc.RpcError as err:
            logging.error(
                f"Error occurred while publishing order: {err.code()}")